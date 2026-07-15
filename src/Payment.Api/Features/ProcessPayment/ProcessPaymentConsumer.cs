using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using MediatR;
using Payment.Api.Infrastructure.EventBus;

namespace Payment.Api.Features.ProcessPayment;

public class ProcessPaymentConsumer(
    IAmazonSQS sqsClient,
    IServiceScopeFactory scopeFactory,
    ILogger<ProcessPaymentConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = Environment.GetEnvironmentVariable("PAYMENT_ORDER_QUEUE_URL");
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            logger.LogWarning("PAYMENT_ORDER_QUEUE_URL is not configured; {Consumer} will not run.", nameof(ProcessPaymentConsumer));
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            ReceiveMessageResponse response;
            try
            {
                response = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = 10,
                    WaitTimeSeconds = 20
                }, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            foreach (var message in response.Messages)
            {
                await ProcessMessageAsync(message, queueUrl, stoppingToken);
            }
        }
    }

    private async Task ProcessMessageAsync(Message message, string queueUrl, CancellationToken cancellationToken)
    {
        try
        {
            var notification = JsonSerializer.Deserialize<SnsNotification>(message.Body, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize SNS notification.");

            var envelope = JsonSerializer.Deserialize<EventEnvelope>(notification.Message, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize event envelope.");

            if (envelope.EventType != "OrderPlaced")
            {
                await sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
                return;
            }

            var data = envelope.Data.Deserialize<OrderPlacedData>(JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize OrderPlaced data.");

            logger.LogInformation("Processing payment for order {OrderId}, customer {CustomerId}, amount {Amount}", data.OrderId, data.CustomerId, data.TotalAmount);

            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new ProcessPaymentCommand(data.OrderId, data.CustomerId, data.TotalAmount), cancellationToken);

            if (!result.Success)
            {
                logger.LogWarning("Payment failed for order {OrderId}: {Message}", data.OrderId, result.Message);
            }

            await sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process SQS message {MessageId}", message.MessageId);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
}

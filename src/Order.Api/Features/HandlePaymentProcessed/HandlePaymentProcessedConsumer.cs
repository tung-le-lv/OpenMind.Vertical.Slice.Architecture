using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using MediatR;
using Order.Api.Domain.Enums;
using Order.Api.Features.UpdateOrderStatus;

namespace Order.Api.Features.HandlePaymentProcessed;

public class HandlePaymentProcessedConsumer(
    IAmazonSQS sqsClient,
    IServiceScopeFactory scopeFactory,
    ILogger<HandlePaymentProcessedConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueUrl = Environment.GetEnvironmentVariable("ORDER_PAYMENT_QUEUE_URL");
        if (string.IsNullOrWhiteSpace(queueUrl))
        {
            logger.LogWarning("ORDER_PAYMENT_QUEUE_URL is not configured; {Consumer} will not run.", nameof(HandlePaymentProcessedConsumer));
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

            if (envelope.EventType != "PaymentProcessed")
            {
                await sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
                return;
            }

            var data = envelope.Data.Deserialize<PaymentProcessedData>(JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize PaymentProcessed data.");

            logger.LogInformation("Updating order {OrderId} status to PaymentConfirmed", data.OrderId);

            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var result = await mediator.Send(new UpdateOrderStatusCommand(data.OrderId, OrderStatus.PaymentConfirmed), cancellationToken);

            if (!result.Success)
            {
                logger.LogWarning("Failed to update order {OrderId}: {Message}", data.OrderId, result.Message);
                return;
            }

            await sqsClient.DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process SQS message {MessageId}", message.MessageId);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private record SnsNotification(string Message);
    private record EventEnvelope(string EventType, JsonElement Data);
    private record PaymentProcessedData(string PaymentId, string OrderId);
}

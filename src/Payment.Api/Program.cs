using Payment.Api.Features.ProcessPayment;
using Payment.Api.Shared;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddCoreServices();
builder.Services.AddEventBus();
builder.Services.AddHostedService<ProcessPaymentConsumer>();

var app = builder.Build();

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

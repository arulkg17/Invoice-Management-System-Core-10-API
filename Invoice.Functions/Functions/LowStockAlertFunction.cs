using Azure.Messaging.ServiceBus;
using Invoice.Events;
using Invoice.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Invoice.Functions.Functions;

/// <summary>
/// TRIGGER: QUEUE "stock-alerts". Tells purchasing to reorder.
/// In a real system this would also create a draft Purchase Order through the API.
/// </summary>
public sealed class LowStockAlertFunction
{
    private readonly IEmailSender _email;
    private readonly ILogger<LowStockAlertFunction> _logger;

    public LowStockAlertFunction(IEmailSender email, ILogger<LowStockAlertFunction> logger)
    {
        _email = email;
        _logger = logger;
    }

    [Function(nameof(LowStockAlertFunction))]
    public async Task Run(
        [ServiceBusTrigger(ServiceBusNames.StockAlertsQueue, Connection = ServiceBusClientFactory.ConnectionName)]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        var alert = message.Body.ToObjectFromJson<LowStockEvent>()
            ?? throw new InvalidOperationException("Message body is not a LowStockEvent.");

        _logger.LogWarning("LOW STOCK: {ItemCode} {ItemName} has {OnHand}, minimum is {Minimum}",
            alert.ItemCode, alert.ItemName, alert.OnHandQuantity, alert.MinimumStock);

        await _email.SendAsync(
            "purchasing@example.com",
            $"Low stock: {alert.ItemCode}",
            $"{alert.ItemName} is down to {alert.OnHandQuantity} (minimum {alert.MinimumStock}) after invoice {alert.TriggeredByInvoiceNumber}. Please reorder.",
            ct: cancellationToken);
    }
}

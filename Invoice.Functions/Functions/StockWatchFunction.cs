using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Invoice.Events;
using Invoice.Functions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Invoice.Functions.Functions;

/// <summary>
/// TRIGGER: subscription "stock-watch" (filter: EventType = SalesInvoicePosted).
///
/// For every item on the posted invoice: is the stock now at or below the item's minimum?
/// If so, send a LowStockEvent to the QUEUE "stock-alerts".
///
/// Topic vs queue: the topic fans one event out to many independent subscribers (email, stock, audit).
/// The queue is point-to-point: one alert, one consumer (LowStockAlertFunction).
/// </summary>
public sealed class StockWatchFunction
{
    private readonly ImsApiClient _api;
    private readonly ServiceBusSender _alerts;
    private readonly ILogger<StockWatchFunction> _logger;

    public StockWatchFunction(ImsApiClient api, ServiceBusClient serviceBus, ILogger<StockWatchFunction> logger)
    {
        _api = api;
        _alerts = serviceBus.CreateSender(ServiceBusNames.StockAlertsQueue);
        _logger = logger;
    }

    [Function(nameof(StockWatchFunction))]
    public async Task Run(
        [ServiceBusTrigger(ServiceBusNames.Topic, ServiceBusNames.StockWatchSubscription, Connection = ServiceBusClientFactory.ConnectionName)]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        var evt = message.Body.ToObjectFromJson<SalesInvoicePostedEvent>()
            ?? throw new InvalidOperationException("Message body is not a SalesInvoicePostedEvent.");

        foreach (var itemId in evt.Lines.Select(l => l.ItemmasterId).Distinct())
        {
            var item = await _api.GetItemAsync(itemId, cancellationToken);
            var stock = await _api.GetStockAsync(itemId, cancellationToken);

            var minimum = item.MinimumStock ?? 0;

            if (stock.OnHandQuantity > minimum)
            {
                _logger.LogInformation("Item {ItemCode}: {OnHand} on hand (minimum {Minimum}). OK.",
                    item.ItemCode, stock.OnHandQuantity, minimum);
                continue;
            }

            var alert = new LowStockEvent
            {
                ItemmasterId = itemId,
                ItemCode = item.ItemCode,
                ItemName = item.ItemName,
                OnHandQuantity = stock.OnHandQuantity,
                MinimumStock = minimum,
                TriggeredByInvoiceNumber = evt.InvoiceNumber
            };

            await _alerts.SendMessageAsync(new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(alert))
            {
                // One alert per item per invoice, even if this function is retried (needs duplicate detection on the queue).
                MessageId = $"lowstock-{itemId}-{evt.InvoiceNumber}",
                Subject = alert.EventType,
                ContentType = "application/json"
            }, cancellationToken);

            _logger.LogWarning("Item {ItemCode} is low: {OnHand} on hand, minimum {Minimum}. Alert queued.",
                item.ItemCode, stock.OnHandQuantity, minimum);
        }
    }
}

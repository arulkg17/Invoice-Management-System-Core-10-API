namespace Invoice.Events;

/// <summary>
/// The one place that knows the names of the messaging entities, so the API, the Functions
/// and the deploy script cannot drift apart.
///
///   Invoice.CqrsAPI --publishes--> TOPIC  ims-events
///                                   |-- subscription invoice-email  (EventType = SalesInvoicePosted)
///                                   |-- subscription stock-watch    (EventType = SalesInvoicePosted)
///                                   |-- subscription audit-log      (every event)
///   Invoice.Functions --sends-->  QUEUE  stock-alerts
/// </summary>
public static class ServiceBusNames
{
    public const string Topic = "ims-events";

    public const string InvoiceEmailSubscription = "invoice-email";
    public const string StockWatchSubscription = "stock-watch";
    public const string AuditLogSubscription = "audit-log";

    public const string StockAlertsQueue = "stock-alerts";

    /// <summary>Name of the application property the subscription filters look at.</summary>
    public const string EventTypeProperty = "EventType";
}

public static class EventTypes
{
    public const string SalesInvoicePosted = "SalesInvoicePosted";
    public const string PurchaseOrderApproved = "PurchaseOrderApproved";
    public const string ReceiptPosted = "ReceiptPosted";
    public const string LowStock = "LowStock";
}

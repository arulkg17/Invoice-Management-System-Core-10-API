namespace Invoice.Events;

/// <summary>
/// A fact that already happened in IMS ("an invoice WAS posted"), named in the past tense.
/// Commands ask for something to happen; events announce that it did. Other systems react to events.
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary>Also used as the Service Bus MessageId, so duplicate detection can drop a double send.</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;

    public abstract string EventType { get; }
}

public sealed record EventLine(int ItemmasterId, decimal Quantity);

public sealed record SalesInvoicePostedEvent : IntegrationEvent
{
    public override string EventType => EventTypes.SalesInvoicePosted;

    public int InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = string.Empty;
    public int CustomerId { get; init; }
    public decimal TotalAmount { get; init; }
    public string? PostedBy { get; init; }
    public IReadOnlyList<EventLine> Lines { get; init; } = Array.Empty<EventLine>();

    /// <summary>True when somebody asked to send it again (the Function's HTTP "resend" endpoint).</summary>
    public bool IsResend { get; init; }
}

public sealed record PurchaseOrderApprovedEvent : IntegrationEvent
{
    public override string EventType => EventTypes.PurchaseOrderApproved;

    public int PurchaseOrderId { get; init; }
    public string PONumber { get; init; } = string.Empty;
    public int VendorId { get; init; }
    public decimal TotalAmount { get; init; }
    public string? ApprovedBy { get; init; }
}

public sealed record ReceiptPostedEvent : IntegrationEvent
{
    public override string EventType => EventTypes.ReceiptPosted;

    public int ReceiptId { get; init; }
    public string? ReceiptNumber { get; init; }
    public int PurchaseOrderId { get; init; }
    public int VendorId { get; init; }
    public decimal TotalAmount { get; init; }
    public string? PostedBy { get; init; }
}

/// <summary>Raised by the stock-watch Function (not by the API) and sent to the stock-alerts queue.</summary>
public sealed record LowStockEvent : IntegrationEvent
{
    public override string EventType => EventTypes.LowStock;

    public int ItemmasterId { get; init; }
    public string ItemCode { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public decimal OnHandQuantity { get; init; }
    public decimal MinimumStock { get; init; }
    public string? TriggeredByInvoiceNumber { get; init; }
}

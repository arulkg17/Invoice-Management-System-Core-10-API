namespace Invoice.CqrsAPI.Common;

/// <summary>Status values used by the BAL for Purchase Orders, Receipts and Sales Invoices.</summary>
public static class DocumentStatus
{
    public const string Draft = "Draft";
    public const string Approved = "Approved";
    public const string Posted = "Posted";
    public const string Cancelled = "Cancelled";
}

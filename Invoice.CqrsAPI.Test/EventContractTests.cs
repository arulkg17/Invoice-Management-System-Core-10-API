using System.Text.Json;
using Invoice.Events;

namespace Invoice.CqrsAPI.Test;

/// <summary>
/// The API serializes events and the Functions deserialize them with BinaryData.ToObjectFromJson.
/// These tests are the safety net for that contract: rename a property and they fail.
/// </summary>
public class EventContractTests
{
    private static BinaryData Wire(IntegrationEvent evt) =>
        new(JsonSerializer.SerializeToUtf8Bytes(evt, evt.GetType()));

    [Fact]
    public void SalesInvoicePosted_survives_the_round_trip()
    {
        var sent = new SalesInvoicePostedEvent
        {
            InvoiceId = 8,
            InvoiceNumber = "INV-000008",
            CustomerId = 2,
            TotalAmount = 354.50m,
            PostedBy = "admin",
            Lines = new[] { new EventLine(7, 3), new EventLine(9, 1.5m) },
            IsResend = true
        };

        var received = Wire(sent).ToObjectFromJson<SalesInvoicePostedEvent>()!;

        Assert.Equal(sent.EventId, received.EventId);
        Assert.Equal(8, received.InvoiceId);
        Assert.Equal("INV-000008", received.InvoiceNumber);
        Assert.Equal(354.50m, received.TotalAmount);
        Assert.True(received.IsResend);
        Assert.Equal(2, received.Lines.Count);
        Assert.Equal(new EventLine(9, 1.5m), received.Lines[1]);
    }

    [Fact]
    public void Derived_properties_are_written_not_just_the_base_class_ones()
    {
        var json = Wire(new LowStockEvent { ItemCode = "ITM-1", OnHandQuantity = 2, MinimumStock = 5 }).ToString();

        Assert.Contains("\"ItemCode\":\"ITM-1\"", json);
        Assert.Contains("\"EventType\":\"LowStock\"", json);
    }

    [Theory]
    [InlineData(typeof(SalesInvoicePostedEvent), EventTypes.SalesInvoicePosted)]
    [InlineData(typeof(PurchaseOrderApprovedEvent), EventTypes.PurchaseOrderApproved)]
    [InlineData(typeof(ReceiptPostedEvent), EventTypes.ReceiptPosted)]
    [InlineData(typeof(LowStockEvent), EventTypes.LowStock)]
    public void Every_event_reports_its_own_type_name(Type eventClass, string expected)
    {
        var evt = (IntegrationEvent)Activator.CreateInstance(eventClass)!;

        Assert.Equal(expected, evt.EventType);
    }
}

using System.Net;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Invoice.Events;
using Invoice.Functions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Invoice.Functions.Functions;

/// <summary>
/// TRIGGER: HTTP.   POST /api/invoices/{invoiceId}/resend
///
/// "The customer lost the email": re-announce the invoice. The same subscribers run again;
/// IsResend = true tells the email function to ignore its "already sent" marker.
/// AuthorizationLevel.Function = the caller must send the function key (?code=... or x-functions-key header).
/// </summary>
public sealed class ResendInvoiceFunction
{
    private readonly ImsApiClient _api;
    private readonly ServiceBusSender _topic;
    private readonly ILogger<ResendInvoiceFunction> _logger;

    public ResendInvoiceFunction(ImsApiClient api, ServiceBusClient serviceBus, ILogger<ResendInvoiceFunction> logger)
    {
        _api = api;
        _topic = serviceBus.CreateSender(ServiceBusNames.Topic);
        _logger = logger;
    }

    [Function(nameof(ResendInvoiceFunction))]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "invoices/{invoiceId:int}/resend")] HttpRequest request,
        int invoiceId,
        CancellationToken cancellationToken)
    {
        SalesInvoiceDtoLite invoice;

        try
        {
            var dto = await _api.GetInvoiceAsync(invoiceId, cancellationToken);
            invoice = new SalesInvoiceDtoLite(dto.Id, dto.InvoiceNumber ?? string.Empty, dto.CustomerId, dto.TotalAmount, dto.Status,
                dto.Details.Select(d => new EventLine(d.ItemmasterId, d.Quantity)).ToList());
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundObjectResult(new { message = $"Invoice {invoiceId} was not found." });
        }

        if (invoice.Status != "Posted")
        {
            return new BadRequestObjectResult(new { message = $"Only Posted invoices can be sent. This one is {invoice.Status}." });
        }

        var evt = new SalesInvoicePostedEvent
        {
            InvoiceId = invoice.Id,
            InvoiceNumber = invoice.Number,
            CustomerId = invoice.CustomerId,
            TotalAmount = invoice.Total,
            PostedBy = "resend",
            Lines = invoice.Lines,
            IsResend = true
        };

        var message = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(evt))
        {
            MessageId = evt.EventId.ToString(),
            Subject = evt.EventType,
            ContentType = "application/json"
        };
        message.ApplicationProperties[ServiceBusNames.EventTypeProperty] = evt.EventType;

        await _topic.SendMessageAsync(message, cancellationToken);

        _logger.LogInformation("Resend requested for invoice {InvoiceNumber}; event {EventId} published.", invoice.Number, evt.EventId);

        // 202 Accepted: the work happens asynchronously, in the subscribers.
        return new ObjectResult(new { message = "Invoice announced again.", eventId = evt.EventId }) { StatusCode = StatusCodes.Status202Accepted };
    }

    private sealed record SalesInvoiceDtoLite(
        int Id, string Number, int CustomerId, decimal Total, string Status, IReadOnlyList<EventLine> Lines);
}

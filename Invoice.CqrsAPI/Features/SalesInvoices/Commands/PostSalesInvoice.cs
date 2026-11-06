using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.CqrsAPI.Messaging;
using Invoice.Events;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.SalesInvoices.Commands;

/// <summary>Draft -> Posted. The BAL checks that there is enough stock and then reduces it.</summary>
public sealed record PostSalesInvoiceCommand(int Id, string PostedBy) : ICommand<Unit>;

public sealed class PostSalesInvoiceValidator : RuleValidator<PostSalesInvoiceCommand>
{
    public PostSalesInvoiceValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class PostSalesInvoiceHandler : ICommandHandler<PostSalesInvoiceCommand, Unit>
{
    private readonly ISalesInvoiceService _invoices;
    private readonly IEventPublisher _events;

    public PostSalesInvoiceHandler(ISalesInvoiceService invoices, IEventPublisher events)
    {
        _invoices = invoices;
        _events = events;
    }

    public async Task<Unit> HandleAsync(PostSalesInvoiceCommand request, CancellationToken cancellationToken)
    {
        var existing = await _invoices.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Sales invoice with Id {request.Id} was not found.");

        if (existing.Status != DocumentStatus.Draft)
        {
            throw new BusinessRuleException($"Only Draft sales invoices can be posted. This one is {existing.Status}.");
        }

        if (!await _invoices.PostAsync(request.Id, request.PostedBy))
        {
            throw new BusinessRuleException("The sales invoice could not be posted.");
        }

        // The invoice is posted and saved. Now tell everybody else (email, stock watch, audit ...).
        // Publishing never throws, so a broker problem cannot undo or hide a successful post.
        await _events.PublishAsync(new SalesInvoicePostedEvent
        {
            InvoiceId = existing.Id,
            InvoiceNumber = existing.InvoiceNumber ?? string.Empty,
            CustomerId = existing.CustomerId,
            TotalAmount = existing.TotalAmount,
            PostedBy = request.PostedBy,
            Lines = existing.Details.Select(d => new EventLine(d.ItemmasterId, d.Quantity)).ToList()
        }, cancellationToken);

        return Unit.Value;
    }
}

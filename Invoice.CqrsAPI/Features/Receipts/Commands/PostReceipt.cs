using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.CqrsAPI.Messaging;
using Invoice.Events;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Receipts.Commands;

/// <summary>Draft -> Posted. Updates the purchase order's received quantities and the stock.</summary>
public sealed record PostReceiptCommand(int Id, string PostedBy) : ICommand<Unit>;

public sealed class PostReceiptValidator : RuleValidator<PostReceiptCommand>
{
    public PostReceiptValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class PostReceiptHandler : ICommandHandler<PostReceiptCommand, Unit>
{
    private readonly IReceiptService _receipts;
    private readonly IEventPublisher _events;

    public PostReceiptHandler(IReceiptService receipts, IEventPublisher events)
    {
        _receipts = receipts;
        _events = events;
    }

    public async Task<Unit> HandleAsync(PostReceiptCommand request, CancellationToken cancellationToken)
    {
        var existing = await _receipts.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Receipt with Id {request.Id} was not found.");

        if (existing.Status != DocumentStatus.Draft)
        {
            throw new BusinessRuleException($"Only Draft receipts can be posted. This one is {existing.Status}.");
        }

        if (!await _receipts.PostAsync(request.Id, request.PostedBy))
        {
            throw new BusinessRuleException("The receipt could not be posted.");
        }

        await _events.PublishAsync(new ReceiptPostedEvent
        {
            ReceiptId = existing.Id,
            ReceiptNumber = existing.ReceiptNumber,
            PurchaseOrderId = existing.PurchaseOrderId,
            VendorId = existing.VendorId,
            TotalAmount = existing.TotalAmount,
            PostedBy = request.PostedBy
        }, cancellationToken);

        return Unit.Value;
    }
}

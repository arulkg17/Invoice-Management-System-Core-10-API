using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Receipts.Commands;

public sealed record DeleteReceiptCommand(int Id, string DeletedBy) : ICommand<Unit>;

public sealed class DeleteReceiptValidator : RuleValidator<DeleteReceiptCommand>
{
    public DeleteReceiptValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class DeleteReceiptHandler : ICommandHandler<DeleteReceiptCommand, Unit>
{
    private readonly IReceiptService _receipts;

    public DeleteReceiptHandler(IReceiptService receipts)
    {
        _receipts = receipts;
    }

    public async Task<Unit> HandleAsync(DeleteReceiptCommand request, CancellationToken cancellationToken)
    {
        var existing = await _receipts.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Receipt with Id {request.Id} was not found.");

        // Business rule: a Posted receipt has already changed stock. Cancel it instead of deleting it.
        if (existing.Status is not (DocumentStatus.Draft or DocumentStatus.Cancelled))
        {
            throw new BusinessRuleException(
                $"A {existing.Status} receipt cannot be deleted. Only Draft or Cancelled ones can.");
        }

        var deleted = await _receipts.DeleteAsync(request.Id, request.DeletedBy);

        if (!deleted)
        {
            throw new NotFoundException($"Receipt with Id {request.Id} was not found.");
        }

        return Unit.Value;
    }
}

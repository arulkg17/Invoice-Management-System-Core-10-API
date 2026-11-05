using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Receipts.Commands;

public sealed record CancelReceiptCommand(int Id, string CancelledBy) : ICommand<Unit>;

public sealed class CancelReceiptValidator : RuleValidator<CancelReceiptCommand>
{
    public CancelReceiptValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class CancelReceiptHandler : ICommandHandler<CancelReceiptCommand, Unit>
{
    private readonly IReceiptService _receipts;

    public CancelReceiptHandler(IReceiptService receipts)
    {
        _receipts = receipts;
    }

    public async Task<Unit> HandleAsync(CancelReceiptCommand request, CancellationToken cancellationToken)
    {
        var existing = await _receipts.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Receipt with Id {request.Id} was not found.");

        if (existing.Status == DocumentStatus.Cancelled)
        {
            throw new BusinessRuleException("This receipt is already cancelled.");
        }

        if (!await _receipts.CancelAsync(request.Id, request.CancelledBy))
        {
            throw new BusinessRuleException("The receipt could not be cancelled.");
        }

        return Unit.Value;
    }
}

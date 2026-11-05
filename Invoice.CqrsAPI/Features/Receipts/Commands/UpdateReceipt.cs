using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Receipts.Commands;

public sealed record UpdateReceiptCommand(int Id, ReceiptData Data, string UpdatedBy)
    : ICommand<Unit>, IHasReceiptData;

public sealed class UpdateReceiptValidator : ReceiptDataValidator<UpdateReceiptCommand>
{
    public UpdateReceiptValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class UpdateReceiptHandler : ICommandHandler<UpdateReceiptCommand, Unit>
{
    private readonly IReceiptService _receipts;

    public UpdateReceiptHandler(IReceiptService receipts)
    {
        _receipts = receipts;
    }

    public async Task<Unit> HandleAsync(UpdateReceiptCommand request, CancellationToken cancellationToken)
    {
        var existing = await _receipts.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Receipt with Id {request.Id} was not found.");

        if (existing.Status != DocumentStatus.Draft)
        {
            throw new BusinessRuleException("Only Draft receipts can be edited.");
        }

        var data = request.Data;

        var dto = new ReceiptDto
        {
            Id = request.Id,
            ReceiptNumber = string.IsNullOrWhiteSpace(data.ReceiptNumber) ? existing.ReceiptNumber : data.ReceiptNumber.Trim(),
            ReceiptDate = data.ReceiptDate,
            PurchaseOrderId = data.PurchaseOrderId,
            Status = existing.Status,
            Notes = data.Notes,
            CreatedBy = existing.CreatedBy,
            CreatedDate = existing.CreatedDate,
            UpdatedBy = request.UpdatedBy,
            Details = data.ToDetails()
        };

        var updated = await _receipts.UpdateAsync(dto);

        if (!updated)
        {
            throw new NotFoundException($"Receipt with Id {request.Id} was not found.");
        }

        return Unit.Value;
    }
}

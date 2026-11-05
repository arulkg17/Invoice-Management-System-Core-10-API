using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Receipts.Commands;

public sealed record CreateReceiptCommand(ReceiptData Data, string CreatedBy)
    : ICommand<int>, IHasReceiptData;

public sealed class CreateReceiptValidator : ReceiptDataValidator<CreateReceiptCommand>
{
}

public sealed class CreateReceiptHandler : ICommandHandler<CreateReceiptCommand, int>
{
    private readonly IReceiptService _receipts;

    public CreateReceiptHandler(IReceiptService receipts)
    {
        _receipts = receipts;
    }

    public async Task<int> HandleAsync(CreateReceiptCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;

        // The BAL already checks: the PO exists and is Approved / PartiallyReceived, every line belongs
        // to it, and nothing is received beyond the outstanding quantity. No need to repeat that here.
        var dto = new ReceiptDto
        {
            // Empty number = let the BAL generate GRN-000001 ...
            ReceiptNumber = string.IsNullOrWhiteSpace(data.ReceiptNumber) ? null : data.ReceiptNumber.Trim(),
            ReceiptDate = data.ReceiptDate,
            PurchaseOrderId = data.PurchaseOrderId,
            Status = DocumentStatus.Draft,
            Notes = data.Notes,
            CreatedBy = request.CreatedBy,
            Details = data.ToDetails()
        };

        return await _receipts.AddAsync(dto);
    }
}

using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Receipts.Queries;

public sealed record GetAllReceiptsQuery : IQuery<IEnumerable<ReceiptDto>>;

public sealed class GetAllReceiptsHandler : IQueryHandler<GetAllReceiptsQuery, IEnumerable<ReceiptDto>>
{
    private readonly IReceiptService _receipts;

    public GetAllReceiptsHandler(IReceiptService receipts)
    {
        _receipts = receipts;
    }

    public Task<IEnumerable<ReceiptDto>> HandleAsync(GetAllReceiptsQuery request, CancellationToken cancellationToken)
    {
        return _receipts.GetAllAsync();
    }
}

public sealed record GetReceiptByIdQuery(int Id) : IQuery<ReceiptDto>;

public sealed class GetReceiptByIdValidator : RuleValidator<GetReceiptByIdQuery>
{
    public GetReceiptByIdValidator()
    {
        Rule(q => q.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class GetReceiptByIdHandler : IQueryHandler<GetReceiptByIdQuery, ReceiptDto>
{
    private readonly IReceiptService _receipts;

    public GetReceiptByIdHandler(IReceiptService receipts)
    {
        _receipts = receipts;
    }

    public async Task<ReceiptDto> HandleAsync(GetReceiptByIdQuery request, CancellationToken cancellationToken)
    {
        return await _receipts.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Receipt with Id {request.Id} was not found.");
    }
}

public sealed record GetReceiptsPagedQuery(
    string? ReceiptNumber,
    int? PurchaseOrderId,
    int? VendorId,
    string? Status,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResultDto<ReceiptDto>>;

public sealed class GetReceiptsPagedValidator : RuleValidator<GetReceiptsPagedQuery>
{
    public GetReceiptsPagedValidator()
    {
        Rule(q => q.PageNumber >= 1, "PageNumber", "Page number must be greater than zero.");
        Rule(q => q.PageSize >= 1, "PageSize", "Page size must be greater than zero.");
        Rule(q => q.PageSize <= ReceiptRules.MaxPageSize, "PageSize",
            $"Page size cannot be greater than {ReceiptRules.MaxPageSize}.");
    }
}

public sealed class GetReceiptsPagedHandler : IQueryHandler<GetReceiptsPagedQuery, PagedResultDto<ReceiptDto>>
{
    private readonly IReceiptService _receipts;

    public GetReceiptsPagedHandler(IReceiptService receipts)
    {
        _receipts = receipts;
    }

    public Task<PagedResultDto<ReceiptDto>> HandleAsync(GetReceiptsPagedQuery request, CancellationToken cancellationToken)
    {
        return _receipts.GetAllPagedAsync(
            request.ReceiptNumber, request.PurchaseOrderId, request.VendorId, request.Status,
            request.PageNumber, request.PageSize);
    }
}

public sealed record GetReceiptPdfQuery(int Id) : IQuery<byte[]>;

public sealed class GetReceiptPdfValidator : RuleValidator<GetReceiptPdfQuery>
{
    public GetReceiptPdfValidator()
    {
        Rule(q => q.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class GetReceiptPdfHandler : IQueryHandler<GetReceiptPdfQuery, byte[]>
{
    private readonly IDocumentPrintService _print;

    public GetReceiptPdfHandler(IDocumentPrintService print)
    {
        _print = print;
    }

    public async Task<byte[]> HandleAsync(GetReceiptPdfQuery request, CancellationToken cancellationToken)
    {
        return await _print.GetReceiptPdfAsync(request.Id)
            ?? throw new NotFoundException($"Receipt with Id {request.Id} was not found.");
    }
}

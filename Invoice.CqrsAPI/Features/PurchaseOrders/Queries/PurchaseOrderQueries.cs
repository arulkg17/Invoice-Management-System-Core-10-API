using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.PurchaseOrders.Queries;

// ---------------- GetAll ----------------

public sealed record GetAllPurchaseOrdersQuery : IQuery<IEnumerable<PurchaseOrderDto>>;

public sealed class GetAllPurchaseOrdersHandler : IQueryHandler<GetAllPurchaseOrdersQuery, IEnumerable<PurchaseOrderDto>>
{
    private readonly IPurchaseOrderService _orders;

    public GetAllPurchaseOrdersHandler(IPurchaseOrderService orders)
    {
        _orders = orders;
    }

    public Task<IEnumerable<PurchaseOrderDto>> HandleAsync(GetAllPurchaseOrdersQuery request, CancellationToken cancellationToken)
    {
        return _orders.GetAllAsync();
    }
}

// ---------------- GetById ----------------

public sealed record GetPurchaseOrderByIdQuery(int Id) : IQuery<PurchaseOrderDto>;

public sealed class GetPurchaseOrderByIdValidator : RuleValidator<GetPurchaseOrderByIdQuery>
{
    public GetPurchaseOrderByIdValidator()
    {
        Rule(q => q.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class GetPurchaseOrderByIdHandler : IQueryHandler<GetPurchaseOrderByIdQuery, PurchaseOrderDto>
{
    private readonly IPurchaseOrderService _orders;

    public GetPurchaseOrderByIdHandler(IPurchaseOrderService orders)
    {
        _orders = orders;
    }

    public async Task<PurchaseOrderDto> HandleAsync(GetPurchaseOrderByIdQuery request, CancellationToken cancellationToken)
    {
        return await _orders.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Purchase order with Id {request.Id} was not found.");
    }
}

// ---------------- GetPaged ----------------

public sealed record GetPurchaseOrdersPagedQuery(
    string? PONumber,
    int? VendorId,
    string? Status,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResultDto<PurchaseOrderDto>>;

public sealed class GetPurchaseOrdersPagedValidator : RuleValidator<GetPurchaseOrdersPagedQuery>
{
    public GetPurchaseOrdersPagedValidator()
    {
        Rule(q => q.PageNumber >= 1, "PageNumber", "Page number must be greater than zero.");
        Rule(q => q.PageSize >= 1, "PageSize", "Page size must be greater than zero.");
        Rule(q => q.PageSize <= PurchaseOrderRules.MaxPageSize, "PageSize",
            $"Page size cannot be greater than {PurchaseOrderRules.MaxPageSize}.");
    }
}

public sealed class GetPurchaseOrdersPagedHandler : IQueryHandler<GetPurchaseOrdersPagedQuery, PagedResultDto<PurchaseOrderDto>>
{
    private readonly IPurchaseOrderService _orders;

    public GetPurchaseOrdersPagedHandler(IPurchaseOrderService orders)
    {
        _orders = orders;
    }

    public Task<PagedResultDto<PurchaseOrderDto>> HandleAsync(GetPurchaseOrdersPagedQuery request, CancellationToken cancellationToken)
    {
        return _orders.GetAllPagedAsync(
            request.PONumber, request.VendorId, request.Status, request.PageNumber, request.PageSize);
    }
}

// ---------------- PDF ----------------

public sealed record GetPurchaseOrderPdfQuery(int Id) : IQuery<byte[]>;

public sealed class GetPurchaseOrderPdfValidator : RuleValidator<GetPurchaseOrderPdfQuery>
{
    public GetPurchaseOrderPdfValidator()
    {
        Rule(q => q.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class GetPurchaseOrderPdfHandler : IQueryHandler<GetPurchaseOrderPdfQuery, byte[]>
{
    private readonly IDocumentPrintService _print;

    public GetPurchaseOrderPdfHandler(IDocumentPrintService print)
    {
        _print = print;
    }

    public async Task<byte[]> HandleAsync(GetPurchaseOrderPdfQuery request, CancellationToken cancellationToken)
    {
        return await _print.GetPurchaseOrderPdfAsync(request.Id)
            ?? throw new NotFoundException($"Purchase order with Id {request.Id} was not found.");
    }
}

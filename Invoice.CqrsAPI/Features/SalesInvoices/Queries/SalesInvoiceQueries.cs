using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.SalesInvoices.Queries;

public sealed record GetAllSalesInvoicesQuery : IQuery<IEnumerable<SalesInvoiceDto>>;

public sealed class GetAllSalesInvoicesHandler : IQueryHandler<GetAllSalesInvoicesQuery, IEnumerable<SalesInvoiceDto>>
{
    private readonly ISalesInvoiceService _invoices;

    public GetAllSalesInvoicesHandler(ISalesInvoiceService invoices)
    {
        _invoices = invoices;
    }

    public Task<IEnumerable<SalesInvoiceDto>> HandleAsync(GetAllSalesInvoicesQuery request, CancellationToken cancellationToken)
    {
        return _invoices.GetAllAsync();
    }
}

public sealed record GetSalesInvoiceByIdQuery(int Id) : IQuery<SalesInvoiceDto>;

public sealed class GetSalesInvoiceByIdValidator : RuleValidator<GetSalesInvoiceByIdQuery>
{
    public GetSalesInvoiceByIdValidator()
    {
        Rule(q => q.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class GetSalesInvoiceByIdHandler : IQueryHandler<GetSalesInvoiceByIdQuery, SalesInvoiceDto>
{
    private readonly ISalesInvoiceService _invoices;

    public GetSalesInvoiceByIdHandler(ISalesInvoiceService invoices)
    {
        _invoices = invoices;
    }

    public async Task<SalesInvoiceDto> HandleAsync(GetSalesInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        return await _invoices.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Sales invoice with Id {request.Id} was not found.");
    }
}

public sealed record GetSalesInvoicesPagedQuery(
    string? InvoiceNumber,
    int? CustomerId,
    string? Status,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResultDto<SalesInvoiceDto>>;

public sealed class GetSalesInvoicesPagedValidator : RuleValidator<GetSalesInvoicesPagedQuery>
{
    public GetSalesInvoicesPagedValidator()
    {
        Rule(q => q.PageNumber >= 1, "PageNumber", "Page number must be greater than zero.");
        Rule(q => q.PageSize >= 1, "PageSize", "Page size must be greater than zero.");
        Rule(q => q.PageSize <= SalesInvoiceRules.MaxPageSize, "PageSize",
            $"Page size cannot be greater than {SalesInvoiceRules.MaxPageSize}.");
    }
}

public sealed class GetSalesInvoicesPagedHandler : IQueryHandler<GetSalesInvoicesPagedQuery, PagedResultDto<SalesInvoiceDto>>
{
    private readonly ISalesInvoiceService _invoices;

    public GetSalesInvoicesPagedHandler(ISalesInvoiceService invoices)
    {
        _invoices = invoices;
    }

    public Task<PagedResultDto<SalesInvoiceDto>> HandleAsync(GetSalesInvoicesPagedQuery request, CancellationToken cancellationToken)
    {
        return _invoices.GetAllPagedAsync(
            request.InvoiceNumber, request.CustomerId, request.Status, request.PageNumber, request.PageSize);
    }
}

public sealed record GetSalesInvoicePdfQuery(int Id) : IQuery<byte[]>;

public sealed class GetSalesInvoicePdfValidator : RuleValidator<GetSalesInvoicePdfQuery>
{
    public GetSalesInvoicePdfValidator()
    {
        Rule(q => q.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class GetSalesInvoicePdfHandler : IQueryHandler<GetSalesInvoicePdfQuery, byte[]>
{
    private readonly IDocumentPrintService _print;

    public GetSalesInvoicePdfHandler(IDocumentPrintService print)
    {
        _print = print;
    }

    public async Task<byte[]> HandleAsync(GetSalesInvoicePdfQuery request, CancellationToken cancellationToken)
    {
        return await _print.GetSalesInvoicePdfAsync(request.Id)
            ?? throw new NotFoundException($"Sales invoice with Id {request.Id} was not found.");
    }
}

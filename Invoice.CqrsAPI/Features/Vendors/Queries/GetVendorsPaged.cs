using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Vendors.Queries;

public sealed record GetVendorsPagedQuery(
    string? VendorCode,
    string? VendorName,
    string? MobileNo,
    string? City,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResultDto<VendorDto>>;

public sealed class GetVendorsPagedValidator : RuleValidator<GetVendorsPagedQuery>
{
    public GetVendorsPagedValidator()
    {
        Rule(q => q.PageNumber >= 1, "PageNumber", "Page number must be greater than zero.");
        Rule(q => q.PageSize >= 1, "PageSize", "Page size must be greater than zero.");
        Rule(q => q.PageSize <= VendorRules.MaxPageSize, "PageSize",
            $"Page size cannot be greater than {VendorRules.MaxPageSize}.");
    }
}

public sealed class GetVendorsPagedHandler : IQueryHandler<GetVendorsPagedQuery, PagedResultDto<VendorDto>>
{
    private readonly IVendorService _vendors;

    public GetVendorsPagedHandler(IVendorService vendors)
    {
        _vendors = vendors;
    }

    public Task<PagedResultDto<VendorDto>> HandleAsync(GetVendorsPagedQuery request, CancellationToken cancellationToken)
    {
        return _vendors.GetAllPagedAsync(
            request.VendorCode, request.VendorName, request.MobileNo, request.City,
            request.PageNumber, request.PageSize);
    }
}

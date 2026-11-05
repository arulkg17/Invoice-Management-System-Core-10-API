using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Vendors.Queries;

public sealed record GetVendorByIdQuery(int Id) : IQuery<VendorDto>;

public sealed class GetVendorByIdValidator : RuleValidator<GetVendorByIdQuery>
{
    public GetVendorByIdValidator()
    {
        Rule(q => q.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class GetVendorByIdHandler : IQueryHandler<GetVendorByIdQuery, VendorDto>
{
    private readonly IVendorService _vendors;

    public GetVendorByIdHandler(IVendorService vendors)
    {
        _vendors = vendors;
    }

    public async Task<VendorDto> HandleAsync(GetVendorByIdQuery request, CancellationToken cancellationToken)
    {
        return await _vendors.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Vendor with Id {request.Id} was not found.");
    }
}

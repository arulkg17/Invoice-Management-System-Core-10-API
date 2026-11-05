using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Vendors.Queries;

public sealed record GetAllVendorsQuery : IQuery<IEnumerable<VendorDto>>;

public sealed class GetAllVendorsHandler : IQueryHandler<GetAllVendorsQuery, IEnumerable<VendorDto>>
{
    private readonly IVendorService _vendors;

    public GetAllVendorsHandler(IVendorService vendors)
    {
        _vendors = vendors;
    }

    public Task<IEnumerable<VendorDto>> HandleAsync(GetAllVendorsQuery request, CancellationToken cancellationToken)
    {
        return _vendors.GetAllAsync();
    }
}

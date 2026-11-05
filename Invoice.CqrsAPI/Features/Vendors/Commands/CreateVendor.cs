using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Vendors.Commands;

public sealed record CreateVendorCommand(VendorData Data, string CreatedBy) : ICommand<int>, IHasVendorData;

public sealed class CreateVendorValidator : VendorDataValidator<CreateVendorCommand>
{
}

public sealed class CreateVendorHandler : ICommandHandler<CreateVendorCommand, int>
{
    private readonly IVendorService _vendors;

    public CreateVendorHandler(IVendorService vendors)
    {
        _vendors = vendors;
    }

    public async Task<int> HandleAsync(CreateVendorCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;
        var code = data.VendorCode.Trim();

        await VendorChecks.EnsureCodeIsUniqueAsync(_vendors, code, excludeId: null);

        var dto = new VendorDto
        {
            VendorCode = code,
            VendorName = data.VendorName.Trim(),
            ContactPerson = data.ContactPerson,
            MobileNo = data.MobileNo,
            Email = data.Email,
            Address1 = data.Address1,
            Address2 = data.Address2,
            City = data.City,
            State = data.State,
            Country = data.Country,
            ZipCode = data.ZipCode,
            GstNo = data.GstNo,
            IsActive = data.IsActive,
            CreatedBy = request.CreatedBy
        };

        return await _vendors.AddAsync(dto);
    }
}

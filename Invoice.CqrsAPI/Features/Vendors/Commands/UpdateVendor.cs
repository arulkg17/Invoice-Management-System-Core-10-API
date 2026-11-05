using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Vendors.Commands;

public sealed record UpdateVendorCommand(int Id, VendorData Data, string UpdatedBy) : ICommand<Unit>, IHasVendorData;

public sealed class UpdateVendorValidator : VendorDataValidator<UpdateVendorCommand>
{
    public UpdateVendorValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class UpdateVendorHandler : ICommandHandler<UpdateVendorCommand, Unit>
{
    private readonly IVendorService _vendors;

    public UpdateVendorHandler(IVendorService vendors)
    {
        _vendors = vendors;
    }

    public async Task<Unit> HandleAsync(UpdateVendorCommand request, CancellationToken cancellationToken)
    {
        // Load -> change -> save: keeps CreatedBy / CreatedDate and gives a clean 404.
        var existing = await _vendors.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Vendor with Id {request.Id} was not found.");

        var data = request.Data;
        var code = data.VendorCode.Trim();

        await VendorChecks.EnsureCodeIsUniqueAsync(_vendors, code, excludeId: request.Id);

        existing.VendorCode = code;
        existing.VendorName = data.VendorName.Trim();
        existing.ContactPerson = data.ContactPerson;
        existing.MobileNo = data.MobileNo;
        existing.Email = data.Email;
        existing.Address1 = data.Address1;
        existing.Address2 = data.Address2;
        existing.City = data.City;
        existing.State = data.State;
        existing.Country = data.Country;
        existing.ZipCode = data.ZipCode;
        existing.GstNo = data.GstNo;
        existing.IsActive = data.IsActive ?? existing.IsActive;
        existing.UpdatedBy = request.UpdatedBy;

        var updated = await _vendors.UpdateAsync(existing);

        if (!updated)
        {
            throw new NotFoundException($"Vendor with Id {request.Id} was not found.");
        }

        return Unit.Value;
    }
}

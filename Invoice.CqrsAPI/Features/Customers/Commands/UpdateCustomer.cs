using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Customers.Commands;

public sealed record UpdateCustomerCommand(int Id, CustomerData Data, string UpdatedBy) : ICommand<Unit>, IHasCustomerData;

public sealed class UpdateCustomerValidator : CustomerDataValidator<UpdateCustomerCommand>
{
    public UpdateCustomerValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class UpdateCustomerHandler : ICommandHandler<UpdateCustomerCommand, Unit>
{
    private readonly ICustomerService _customers;

    public UpdateCustomerHandler(ICustomerService customers)
    {
        _customers = customers;
    }

    public async Task<Unit> HandleAsync(UpdateCustomerCommand request, CancellationToken cancellationToken)
    {
        // Load -> change -> save: keeps CreatedBy / CreatedDate and gives a clean 404.
        var existing = await _customers.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Customer with Id {request.Id} was not found.");

        var data = request.Data;
        var code = data.CustomerCode.Trim();

        await CustomerChecks.EnsureCodeIsUniqueAsync(_customers, code, excludeId: request.Id);

        existing.CustomerCode = code;
        existing.CustomerName = data.CustomerName.Trim();
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

        var updated = await _customers.UpdateAsync(existing);

        if (!updated)
        {
            throw new NotFoundException($"Customer with Id {request.Id} was not found.");
        }

        return Unit.Value;
    }
}

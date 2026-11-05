using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Customers.Commands;

public sealed record CreateCustomerCommand(CustomerData Data, string CreatedBy) : ICommand<int>, IHasCustomerData;

public sealed class CreateCustomerValidator : CustomerDataValidator<CreateCustomerCommand>
{
}

public sealed class CreateCustomerHandler : ICommandHandler<CreateCustomerCommand, int>
{
    private readonly ICustomerService _customers;

    public CreateCustomerHandler(ICustomerService customers)
    {
        _customers = customers;
    }

    public async Task<int> HandleAsync(CreateCustomerCommand request, CancellationToken cancellationToken)
    {
        var data = request.Data;
        var code = data.CustomerCode.Trim();

        await CustomerChecks.EnsureCodeIsUniqueAsync(_customers, code, excludeId: null);

        var dto = new CustomerDto
        {
            CustomerCode = code,
            CustomerName = data.CustomerName.Trim(),
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

        return await _customers.AddAsync(dto);
    }
}

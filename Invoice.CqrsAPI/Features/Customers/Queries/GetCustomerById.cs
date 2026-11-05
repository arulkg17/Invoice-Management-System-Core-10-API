using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Customers.Queries;

public sealed record GetCustomerByIdQuery(int Id) : IQuery<CustomerDto>;

public sealed class GetCustomerByIdValidator : RuleValidator<GetCustomerByIdQuery>
{
    public GetCustomerByIdValidator()
    {
        Rule(q => q.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class GetCustomerByIdHandler : IQueryHandler<GetCustomerByIdQuery, CustomerDto>
{
    private readonly ICustomerService _customers;

    public GetCustomerByIdHandler(ICustomerService customers)
    {
        _customers = customers;
    }

    public async Task<CustomerDto> HandleAsync(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        return await _customers.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Customer with Id {request.Id} was not found.");
    }
}

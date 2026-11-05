using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Customers.Queries;

public sealed record GetAllCustomersQuery : IQuery<IEnumerable<CustomerDto>>;

public sealed class GetAllCustomersHandler : IQueryHandler<GetAllCustomersQuery, IEnumerable<CustomerDto>>
{
    private readonly ICustomerService _customers;

    public GetAllCustomersHandler(ICustomerService customers)
    {
        _customers = customers;
    }

    public Task<IEnumerable<CustomerDto>> HandleAsync(GetAllCustomersQuery request, CancellationToken cancellationToken)
    {
        return _customers.GetAllAsync();
    }
}

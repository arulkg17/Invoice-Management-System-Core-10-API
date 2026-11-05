using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Customers.Queries;

public sealed record GetCustomersPagedQuery(
    string? CustomerCode,
    string? CustomerName,
    string? MobileNo,
    string? City,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResultDto<CustomerDto>>;

public sealed class GetCustomersPagedValidator : RuleValidator<GetCustomersPagedQuery>
{
    public GetCustomersPagedValidator()
    {
        Rule(q => q.PageNumber >= 1, "PageNumber", "Page number must be greater than zero.");
        Rule(q => q.PageSize >= 1, "PageSize", "Page size must be greater than zero.");
        Rule(q => q.PageSize <= CustomerRules.MaxPageSize, "PageSize",
            $"Page size cannot be greater than {CustomerRules.MaxPageSize}.");
    }
}

public sealed class GetCustomersPagedHandler : IQueryHandler<GetCustomersPagedQuery, PagedResultDto<CustomerDto>>
{
    private readonly ICustomerService _customers;

    public GetCustomersPagedHandler(ICustomerService customers)
    {
        _customers = customers;
    }

    public Task<PagedResultDto<CustomerDto>> HandleAsync(GetCustomersPagedQuery request, CancellationToken cancellationToken)
    {
        return _customers.GetAllPagedAsync(
            request.CustomerCode, request.CustomerName, request.MobileNo, request.City,
            request.PageNumber, request.PageSize);
    }
}

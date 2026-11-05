using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Users.Queries;

// ---------------- GetAll ----------------

public sealed record GetAllUsersQuery : IQuery<IEnumerable<UserDto>>;

public sealed class GetAllUsersHandler : IQueryHandler<GetAllUsersQuery, IEnumerable<UserDto>>
{
    private readonly IUserService _users;

    public GetAllUsersHandler(IUserService users)
    {
        _users = users;
    }

    public async Task<IEnumerable<UserDto>> HandleAsync(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        return UserResponses.Unwrap(await _users.GetAllAsync());
    }
}

// ---------------- GetById ----------------

public sealed record GetUserByIdQuery(int Id) : IQuery<UserDto>;

public sealed class GetUserByIdValidator : RuleValidator<GetUserByIdQuery>
{
    public GetUserByIdValidator()
    {
        Rule(q => q.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class GetUserByIdHandler : IQueryHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserService _users;

    public GetUserByIdHandler(IUserService users)
    {
        _users = users;
    }

    public async Task<UserDto> HandleAsync(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        return UserResponses.Unwrap(await _users.GetByIdAsync(request.Id));
    }
}

// ---------------- GetPaged ----------------

public sealed record GetUsersPagedQuery(
    string? UserName,
    string? Email,
    string? FirstName,
    string? LastName,
    bool? IsActive,
    int PageNumber = 1,
    int PageSize = 10) : IQuery<PagedResultDto<UserDto>>;

public sealed class GetUsersPagedValidator : RuleValidator<GetUsersPagedQuery>
{
    public GetUsersPagedValidator()
    {
        Rule(q => q.PageNumber >= 1, "PageNumber", "Page number must be greater than zero.");
        Rule(q => q.PageSize >= 1, "PageSize", "Page size must be greater than zero.");
        Rule(q => q.PageSize <= UserRules.MaxPageSize, "PageSize",
            $"Page size cannot be greater than {UserRules.MaxPageSize}.");
    }
}

public sealed class GetUsersPagedHandler : IQueryHandler<GetUsersPagedQuery, PagedResultDto<UserDto>>
{
    private readonly IUserService _users;

    public GetUsersPagedHandler(IUserService users)
    {
        _users = users;
    }

    public async Task<PagedResultDto<UserDto>> HandleAsync(GetUsersPagedQuery request, CancellationToken cancellationToken)
    {
        var filter = new UserFilterDto
        {
            UserName = request.UserName,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = request.IsActive,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return UserResponses.Unwrap(await _users.GetAllPagedAsync(filter));
    }
}

// ---------------- ValidateUser (check a user name + password, issue nothing) ----------------

public sealed record ValidateUserQuery(string UserName, string Password) : IQuery<UserDto>;

public sealed class ValidateUserValidator : RuleValidator<ValidateUserQuery>
{
    public ValidateUserValidator()
    {
        Rule(q => !string.IsNullOrWhiteSpace(q.UserName), "UserName", "User name is required.");
        Rule(q => !string.IsNullOrWhiteSpace(q.Password), "Password", "Password is required.");
    }
}

public sealed class ValidateUserHandler : IQueryHandler<ValidateUserQuery, UserDto>
{
    private readonly IUserService _users;

    public ValidateUserHandler(IUserService users)
    {
        _users = users;
    }

    public async Task<UserDto> HandleAsync(ValidateUserQuery request, CancellationToken cancellationToken)
    {
        return await _users.ValidateUserAsync(request.UserName, request.Password)
            ?? throw new InvalidCredentialsException("Invalid username or password.");
    }
}

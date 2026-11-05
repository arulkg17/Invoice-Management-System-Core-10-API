using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Users.Commands;

/// <param name="RequestedByUserId">Id of the logged-in user, taken from the JWT. Null when unknown.</param>
public sealed record DeleteUserCommand(int Id, int? RequestedByUserId) : ICommand<Unit>;

public sealed class DeleteUserValidator : RuleValidator<DeleteUserCommand>
{
    public DeleteUserValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
    }
}

public sealed class DeleteUserHandler : ICommandHandler<DeleteUserCommand, Unit>
{
    private readonly IUserService _users;

    public DeleteUserHandler(IUserService users)
    {
        _users = users;
    }

    public async Task<Unit> HandleAsync(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        // Business rule: nobody can delete the account they are logged in with.
        if (request.RequestedByUserId == request.Id)
        {
            throw new BusinessRuleException("You cannot delete your own account.");
        }

        UserResponses.Unwrap(await _users.DeleteAsync(request.Id));

        return Unit.Value;
    }
}

using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.CqrsAPI.Infrastructure;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Auth;

// A login is modelled as a COMMAND (it has a side effect: a new token is issued).

public sealed record LoginCommand(string UserName, string Password) : ICommand<LoginResponseDto>;

public sealed class LoginValidator : RuleValidator<LoginCommand>
{
    public LoginValidator()
    {
        Rule(c => !string.IsNullOrWhiteSpace(c.UserName), nameof(LoginCommand.UserName), "User name is required.");
        Rule(c => !string.IsNullOrWhiteSpace(c.Password), nameof(LoginCommand.Password), "Password is required.");
    }
}

public sealed class LoginHandler : ICommandHandler<LoginCommand, LoginResponseDto>
{
    private readonly IUserService _users;
    private readonly IJwtTokenService _tokens;

    public LoginHandler(IUserService users, IJwtTokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    public async Task<LoginResponseDto> HandleAsync(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.ValidateUserAsync(request.UserName, request.Password);

        if (user == null)
        {
            throw new InvalidCredentialsException("Invalid username or password.");
        }

        var (token, expiration) = _tokens.CreateToken(user);

        return new LoginResponseDto
        {
            Token = token,
            Expiration = expiration,
            User = user
        };
    }
}

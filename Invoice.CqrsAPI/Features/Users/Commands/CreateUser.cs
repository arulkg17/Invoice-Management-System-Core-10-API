using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Users.Commands;

public sealed record CreateUserCommand(UserProfileData Profile, string Password, string CreatedBy)
    : ICommand<UserDto>, IHasUserProfile;

public sealed class CreateUserValidator : UserProfileValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        Rule(c => IsStrongEnough(c.Password), "Password",
            $"Password must be {UserRules.PasswordMinLength} to {UserRules.PasswordMaxLength} characters.");
    }
}

public sealed class CreateUserHandler : ICommandHandler<CreateUserCommand, UserDto>
{
    private readonly IUserService _users;

    public CreateUserHandler(IUserService users)
    {
        _users = users;
    }

    public async Task<UserDto> HandleAsync(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var p = request.Profile;

        var dto = new UserCreateDto
        {
            UserName = p.UserName.Trim(),
            Email = p.Email.Trim(),
            Password = request.Password,
            FirstName = p.FirstName.Trim(),
            MiddleName = p.MiddleName,
            LastName = p.LastName.Trim(),
            DisplayName = p.EffectiveDisplayName,
            PhoneNumber = p.PhoneNumber ?? string.Empty,
            AlternatePhone = p.AlternatePhone,
            AddressLine1 = p.AddressLine1 ?? string.Empty,
            AddressLine2 = p.AddressLine2,
            City = p.City ?? string.Empty,
            State = p.State ?? string.Empty,
            ZipCode = p.ZipCode ?? string.Empty,
            Country = p.Country ?? string.Empty,
            DateOfBirth = p.DateOfBirth,
            IsActive = p.IsActive,
            CreatedBy = request.CreatedBy
        };

        var response = await _users.CreateAsync(dto);

        if (!response.Success)
        {
            // "Username already exists." / "Email already exists."
            throw new ConflictException(response.Message ?? "The user already exists.");
        }

        return response.Data!;
    }
}

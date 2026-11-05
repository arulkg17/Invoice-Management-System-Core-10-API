using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;

namespace Invoice.CqrsAPI.Features.Users.Commands;

/// <param name="Password">Optional. Leave empty to keep the current password.</param>
public sealed record UpdateUserCommand(int Id, UserProfileData Profile, string? Password, string UpdatedBy)
    : ICommand<UserDto>, IHasUserProfile;

public sealed class UpdateUserValidator : UserProfileValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        Rule(c => c.Id > 0, "Id", "Id must be greater than zero.");
        Rule(c => string.IsNullOrWhiteSpace(c.Password) || IsStrongEnough(c.Password), "Password",
            $"Password must be {UserRules.PasswordMinLength} to {UserRules.PasswordMaxLength} characters.");
    }
}

public sealed class UpdateUserHandler : ICommandHandler<UpdateUserCommand, UserDto>
{
    private readonly IUserService _users;

    public UpdateUserHandler(IUserService users)
    {
        _users = users;
    }

    public async Task<UserDto> HandleAsync(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var p = request.Profile;

        var dto = new UserUpdateDto
        {
            UserName = p.UserName.Trim(),
            Email = p.Email.Trim(),
            Password = string.IsNullOrWhiteSpace(request.Password) ? null : request.Password,
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
            UpdatedBy = request.UpdatedBy
        };

        var response = await _users.UpdateAsync(request.Id, dto);

        return UserResponses.Unwrap(response);
    }
}

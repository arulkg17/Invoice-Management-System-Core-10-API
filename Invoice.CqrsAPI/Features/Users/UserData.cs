using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Cqrs;
using Invoice.DTOs;
using Invoice.Model;

namespace Invoice.CqrsAPI.Features.Users;

/// <summary>The profile fields of a user (everything except the password and the audit columns).</summary>
public sealed record UserProfileData(
    string UserName,
    string Email,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? DisplayName,
    string? PhoneNumber,
    string? AlternatePhone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? State,
    string? ZipCode,
    string? Country,
    DateTime? DateOfBirth,
    bool IsActive)
{
    public static UserProfileData From(UserCreateDto d) => new(
        d.UserName, d.Email, d.FirstName, d.MiddleName, d.LastName, d.DisplayName,
        d.PhoneNumber, d.AlternatePhone, d.AddressLine1, d.AddressLine2, d.City,
        d.State, d.ZipCode, d.Country, d.DateOfBirth, d.IsActive);

    public static UserProfileData From(UserUpdateDto d) => new(
        d.UserName, d.Email, d.FirstName, d.MiddleName, d.LastName, d.DisplayName,
        d.PhoneNumber, d.AlternatePhone, d.AddressLine1, d.AddressLine2, d.City,
        d.State, d.ZipCode, d.Country, d.DateOfBirth, d.IsActive);

    /// <summary>If no display name was given, use "First Last".</summary>
    public string EffectiveDisplayName =>
        string.IsNullOrWhiteSpace(DisplayName) ? $"{FirstName} {LastName}".Trim() : DisplayName.Trim();
}

public interface IHasUserProfile
{
    UserProfileData Profile { get; }
}

public static class UserRules
{
    public const int UserNameMinLength = 3;
    public const int UserNameMaxLength = 50;
    public const int EmailMaxLength = 256;
    public const int NameMaxLength = 100;
    public const int TextMaxLength = 200;
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 100;
    public const int MaxPageSize = 500;

    public const string NotFoundMessage = "User not found.";
}

public abstract class UserProfileValidator<T> : RuleValidator<T> where T : IHasUserProfile
{
    protected UserProfileValidator()
    {
        Rule(c => !string.IsNullOrWhiteSpace(c.Profile.UserName), "UserName", "User name is required.");
        Rule(c => string.IsNullOrWhiteSpace(c.Profile.UserName)
                  || (c.Profile.UserName.Trim().Length >= UserRules.UserNameMinLength
                      && c.Profile.UserName.Trim().Length <= UserRules.UserNameMaxLength),
            "UserName", $"User name must be {UserRules.UserNameMinLength} to {UserRules.UserNameMaxLength} characters.");
        Rule(c => c.Profile.UserName is null || !c.Profile.UserName.Trim().Any(char.IsWhiteSpace),
            "UserName", "User name cannot contain spaces.");

        Rule(c => !string.IsNullOrWhiteSpace(c.Profile.Email), "Email", "Email is required.");
        Rule(c => FieldChecks.IsEmailOrEmpty(c.Profile.Email), "Email", "Email is not a valid address.");
        Rule(c => FieldChecks.MaxLength(c.Profile.Email, UserRules.EmailMaxLength), "Email", "Email is too long.");

        Rule(c => !string.IsNullOrWhiteSpace(c.Profile.FirstName), "FirstName", "First name is required.");
        Rule(c => FieldChecks.MaxLength(c.Profile.FirstName, UserRules.NameMaxLength), "FirstName", "First name is too long.");
        Rule(c => FieldChecks.MaxLength(c.Profile.MiddleName, UserRules.NameMaxLength), "MiddleName", "Middle name is too long.");
        Rule(c => !string.IsNullOrWhiteSpace(c.Profile.LastName), "LastName", "Last name is required.");
        Rule(c => FieldChecks.MaxLength(c.Profile.LastName, UserRules.NameMaxLength), "LastName", "Last name is too long.");
        Rule(c => FieldChecks.MaxLength(c.Profile.DisplayName, UserRules.NameMaxLength), "DisplayName", "Display name is too long.");

        Rule(c => FieldChecks.IsPhoneOrEmpty(c.Profile.PhoneNumber), "PhoneNumber", "Phone number is not valid.");
        Rule(c => FieldChecks.IsPhoneOrEmpty(c.Profile.AlternatePhone), "AlternatePhone", "Alternate phone is not valid.");

        Rule(c => FieldChecks.MaxLength(c.Profile.AddressLine1, UserRules.TextMaxLength), "AddressLine1", "Address line 1 is too long.");
        Rule(c => FieldChecks.MaxLength(c.Profile.AddressLine2, UserRules.TextMaxLength), "AddressLine2", "Address line 2 is too long.");
        Rule(c => FieldChecks.MaxLength(c.Profile.City, UserRules.TextMaxLength), "City", "City is too long.");
        Rule(c => FieldChecks.MaxLength(c.Profile.State, UserRules.TextMaxLength), "State", "State is too long.");
        Rule(c => FieldChecks.MaxLength(c.Profile.Country, UserRules.TextMaxLength), "Country", "Country is too long.");
        Rule(c => FieldChecks.MaxLength(c.Profile.ZipCode, 20), "ZipCode", "Zip code is too long.");

        Rule(c => c.Profile.DateOfBirth is null || c.Profile.DateOfBirth.Value.Date <= DateTime.UtcNow.Date,
            "DateOfBirth", "Date of birth cannot be in the future.");
    }

    protected static bool IsStrongEnough(string? password) =>
        password is not null
        && password.Length >= UserRules.PasswordMinLength
        && password.Length <= UserRules.PasswordMaxLength;
}

/// <summary>
/// IUserService answers with ApiResponse objects instead of throwing. These helpers turn a failed
/// response into the right exception, so the middleware can give the right HTTP status.
/// </summary>
public static class UserResponses
{
    public static T Unwrap<T>(ApiResponse<T> response)
    {
        if (response.Success)
        {
            return response.Data!;
        }

        if (response.Message == UserRules.NotFoundMessage)
        {
            throw new NotFoundException(response.Message);
        }

        throw new BusinessRuleException(response.Message ?? "The request could not be completed.");
    }
}

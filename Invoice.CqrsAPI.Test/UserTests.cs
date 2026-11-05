using Invoice.BAL.Contracts;
using Invoice.CqrsAPI.Common;
using Invoice.CqrsAPI.Features.Users;
using Invoice.CqrsAPI.Features.Users.Commands;
using Invoice.CqrsAPI.Features.Users.Queries;
using Invoice.DTOs;
using Invoice.Model;
using Moq;

namespace Invoice.CqrsAPI.Test;

public class UserTests
{
    private readonly Mock<IUserService> _users = new();

    private static UserProfileData Profile(
        string userName = "jdoe", string email = "jdoe@example.com", string? displayName = null)
        => new(userName, email, "John", null, "Doe", displayName, null, null, null, null, null, null, null, null, null, true);

    // ---------------- validation ----------------

    [Fact]
    public void Valid_create_has_no_failures()
    {
        Assert.Empty(new CreateUserValidator().Validate(new CreateUserCommand(Profile(), "Passw0rd!", "admin")));
    }

    [Fact]
    public void Create_with_short_password_bad_email_and_spaced_user_name_fails()
    {
        var failures = new CreateUserValidator()
            .Validate(new CreateUserCommand(Profile("john doe", "nope"), "123", "admin"));

        Assert.Contains(failures, f => f.Property == "Password");
        Assert.Contains(failures, f => f.Property == "Email");
        Assert.Contains(failures, f => f.Property == "UserName");
    }

    [Fact]
    public void Update_without_password_is_valid_but_a_short_one_is_not()
    {
        var validator = new UpdateUserValidator();

        Assert.Empty(validator.Validate(new UpdateUserCommand(3, Profile(), null, "admin")));
        Assert.Contains(validator.Validate(new UpdateUserCommand(3, Profile(), "123", "admin")), f => f.Property == "Password");
    }

    // ---------------- create ----------------

    [Fact]
    public async Task Create_duplicate_user_throws_conflict()
    {
        _users.Setup(u => u.CreateAsync(It.IsAny<UserCreateDto>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Success = false, Message = "Username already exists." });

        var handler = new CreateUserHandler(_users.Object);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.HandleAsync(new CreateUserCommand(Profile(), "Passw0rd!", "admin"), CancellationToken.None));

        Assert.Equal("Username already exists.", ex.Message);
    }

    [Fact]
    public async Task Create_uses_first_and_last_name_when_display_name_is_blank()
    {
        _users.Setup(u => u.CreateAsync(It.IsAny<UserCreateDto>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Success = true, Data = new UserDto { Id = 4 } });

        var user = await new CreateUserHandler(_users.Object)
            .HandleAsync(new CreateUserCommand(Profile(), "Passw0rd!", "admin"), CancellationToken.None);

        Assert.Equal(4, user.Id);
        _users.Verify(u => u.CreateAsync(It.Is<UserCreateDto>(d =>
            d.DisplayName == "John Doe" && d.CreatedBy == "admin")), Times.Once);
    }

    // ---------------- update ----------------

    [Fact]
    public async Task Update_unknown_user_throws_not_found()
    {
        _users.Setup(u => u.UpdateAsync(7, It.IsAny<UserUpdateDto>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Success = false, Message = "User not found." });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new UpdateUserHandler(_users.Object)
                .HandleAsync(new UpdateUserCommand(7, Profile(), null, "admin"), CancellationToken.None));
    }

    [Fact]
    public async Task Update_other_failure_throws_business_rule()
    {
        _users.Setup(u => u.UpdateAsync(7, It.IsAny<UserUpdateDto>()))
            .ReturnsAsync(new ApiResponse<UserDto> { Success = false, Message = "Unable to update user." });

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new UpdateUserHandler(_users.Object)
                .HandleAsync(new UpdateUserCommand(7, Profile(), null, "admin"), CancellationToken.None));
    }

    // ---------------- delete ----------------

    [Fact]
    public async Task Delete_own_account_is_rejected_without_calling_the_service()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            new DeleteUserHandler(_users.Object)
                .HandleAsync(new DeleteUserCommand(3, RequestedByUserId: 3), CancellationToken.None));

        _users.Verify(u => u.DeleteAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Delete_another_user_calls_the_service()
    {
        _users.Setup(u => u.DeleteAsync(8))
            .ReturnsAsync(new ApiResponse<bool> { Success = true, Data = true });

        await new DeleteUserHandler(_users.Object)
            .HandleAsync(new DeleteUserCommand(8, RequestedByUserId: 3), CancellationToken.None);

        _users.Verify(u => u.DeleteAsync(8), Times.Once);
    }

    [Fact]
    public async Task Delete_unknown_user_throws_not_found()
    {
        _users.Setup(u => u.DeleteAsync(8))
            .ReturnsAsync(new ApiResponse<bool> { Success = false, Message = "User not found." });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new DeleteUserHandler(_users.Object)
                .HandleAsync(new DeleteUserCommand(8, RequestedByUserId: 3), CancellationToken.None));
    }

    // ---------------- queries ----------------

    [Fact]
    public async Task GetById_unknown_user_throws_not_found()
    {
        _users.Setup(u => u.GetByIdAsync(8))
            .ReturnsAsync(new ApiResponse<UserDto> { Success = false, Message = "User not found." });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new GetUserByIdHandler(_users.Object).HandleAsync(new GetUserByIdQuery(8), CancellationToken.None));
    }

    [Fact]
    public async Task ValidateUser_wrong_password_throws_invalid_credentials()
    {
        _users.Setup(u => u.ValidateUserAsync("jdoe", "bad")).ReturnsAsync((UserDto?)null);

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            new ValidateUserHandler(_users.Object)
                .HandleAsync(new ValidateUserQuery("jdoe", "bad"), CancellationToken.None));
    }
}

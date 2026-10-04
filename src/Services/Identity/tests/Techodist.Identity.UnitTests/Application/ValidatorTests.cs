using Techodist.Identity.Application.Features.Auth.Commands.AuthenticateAdmin;
using Techodist.Identity.Application.Features.Auth.Commands.ChangeOwnPassword;
using Techodist.Identity.Application.Features.Users.Commands.CreateAdminUser;
using Techodist.Identity.Application.Features.Users.Commands.UpdateAdminUser;
using Xunit;

namespace Techodist.Identity.UnitTests.Application;

public sealed class ValidatorTests
{
    private static readonly string[] AdminRole = ["Admin"];

    [Fact]
    public void CreateAdminUser_ValidCommand_Passes()
    {
        var result = new CreateAdminUserCommandValidator().Validate(
            new CreateAdminUserCommand("admin@techodist.local", "Администратор", "Techodist!Dev2026", AdminRole));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateAdminUser_InvalidEmail_Fails()
    {
        var result = new CreateAdminUserCommandValidator().Validate(
            new CreateAdminUserCommand("not-an-email", "Администратор", "Techodist!Dev2026", AdminRole));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAdminUserCommand.Email));
    }

    [Fact]
    public void CreateAdminUser_NoRoles_Fails()
    {
        var result = new CreateAdminUserCommandValidator().Validate(
            new CreateAdminUserCommand("admin@techodist.local", "Администратор", "Techodist!Dev2026", []));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAdminUserCommand.Roles));
    }

    [Fact]
    public void CreateAdminUser_UnknownRole_Fails()
    {
        var result = new CreateAdminUserCommandValidator().Validate(
            new CreateAdminUserCommand("admin@techodist.local", "Администратор", "Techodist!Dev2026", ["Customer"]));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAdminUserCommand.Roles));
    }

    [Fact]
    public void CreateAdminUser_EmptyPassword_Fails()
    {
        var result = new CreateAdminUserCommandValidator().Validate(
            new CreateAdminUserCommand("admin@techodist.local", "Администратор", "", AdminRole));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAdminUserCommand.Password));
    }

    [Fact]
    public void UpdateAdminUser_EmptyDisplayName_Fails()
    {
        var result = new UpdateAdminUserCommandValidator().Validate(
            new UpdateAdminUserCommand(Guid.NewGuid(), "   ", AdminRole));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateAdminUserCommand.DisplayName));
    }

    [Fact]
    public void ChangeOwnPassword_SameNewAndCurrentPassword_Fails()
    {
        var result = new ChangeOwnPasswordCommandValidator().Validate(
            new ChangeOwnPasswordCommand(Guid.NewGuid(), "Techodist!Dev2026", "Techodist!Dev2026"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Новый пароль должен отличаться от текущего.");
    }

    [Fact]
    public void ChangeOwnPassword_EmptyUserId_Fails()
    {
        var result = new ChangeOwnPasswordCommandValidator().Validate(
            new ChangeOwnPasswordCommand(Guid.Empty, "Techodist!Dev2026", "Techodist!Dev2027"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangeOwnPasswordCommand.UserId));
    }

    [Fact]
    public void AuthenticateAdmin_EmptyPassword_Fails()
    {
        var result = new AuthenticateAdminCommandValidator().Validate(
            new AuthenticateAdminCommand("admin@techodist.local", ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AuthenticateAdminCommand.Password));
    }
}

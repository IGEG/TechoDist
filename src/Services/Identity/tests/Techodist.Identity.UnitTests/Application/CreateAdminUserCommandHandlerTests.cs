using Microsoft.Extensions.Logging.Abstractions;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.Identity.Application.Features.Users.Commands.CreateAdminUser;
using Techodist.Identity.UnitTests.Fakes;
using Xunit;

namespace Techodist.Identity.UnitTests.Application;

public sealed class CreateAdminUserCommandHandlerTests
{
    private const string StrongPassword = "Techodist!Dev2026";

    private static CreateAdminUserCommandHandler CreateHandler(FakeIdentityService identity)
        => new(identity, NullLogger<CreateAdminUserCommandHandler>.Instance);

    [Fact]
    public async Task Handle_ValidCommand_NormalizesEmailDisplayNameAndRoles()
    {
        var identity = new FakeIdentityService();

        var result = await CreateHandler(identity).Handle(
            new CreateAdminUserCommand(
                "  NewAdmin@Techodist.Local ",
                "  Новый администратор  ",
                StrongPassword,
                ["manager", "ADMIN", "admin"]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var created = identity.LastCreated;
        Assert.NotNull(created);
        Assert.Equal("newadmin@techodist.local", created!.Email);
        Assert.Equal("Новый администратор", created.DisplayName);
        Assert.Equal(new[] { "Admin", "Manager" }, created.Roles);
        Assert.Equal(created.Id, result.Value);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsValidationError()
    {
        var identity = new FakeIdentityService();

        var result = await CreateHandler(identity).Handle(
            new CreateAdminUserCommand("not-an-email", "Администратор", StrongPassword, ["Admin"]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.user.invalid_email", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Null(identity.LastCreated);
    }

    [Fact]
    public async Task Handle_UnknownRole_ReturnsValidationError()
    {
        var identity = new FakeIdentityService();

        var result = await CreateHandler(identity).Handle(
            new CreateAdminUserCommand("admin@techodist.local", "Администратор", StrongPassword, ["Customer"]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.user.invalid_role", result.Error.Code);
        Assert.Null(identity.LastCreated);
    }

    [Fact]
    public async Task Handle_NoRoles_ReturnsRolesRequiredError()
    {
        var identity = new FakeIdentityService();

        var result = await CreateHandler(identity).Handle(
            new CreateAdminUserCommand("admin@techodist.local", "Администратор", StrongPassword, []),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.user.roles_required", result.Error.Code);
        Assert.Null(identity.LastCreated);
    }

    [Fact]
    public async Task Handle_WeakPassword_ReturnsWeakPasswordError()
    {
        var identity = new FakeIdentityService();

        var result = await CreateHandler(identity).Handle(
            new CreateAdminUserCommand("admin@techodist.local", "Администратор", "admin", ["Admin"]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.password.weak", result.Error.Code);
        Assert.Null(identity.LastCreated);
    }

    [Fact]
    public async Task Handle_ServiceFailure_PropagatesError()
    {
        var identity = new FakeIdentityService
        {
            Failure = Error.Validation("identity.user.create_failed", "Пользователь с таким e-mail уже существует."),
        };

        var result = await CreateHandler(identity).Handle(
            new CreateAdminUserCommand("admin@techodist.local", "Администратор", StrongPassword, ["Admin"]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.user.create_failed", result.Error.Code);
        Assert.Null(identity.LastCreated);
    }
}

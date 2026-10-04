using Microsoft.Extensions.Logging.Abstractions;
using Techodist.Identity.Application.Features.Users.Commands.UpdateAdminUser;
using Techodist.Identity.UnitTests.Fakes;
using Xunit;

namespace Techodist.Identity.UnitTests.Application;

public sealed class UpdateAdminUserCommandHandlerTests
{
    private static UpdateAdminUserCommandHandler CreateHandler(
        FakeIdentityService identity,
        Guid? currentUserId = null)
        => new(
            identity,
            new FakeCurrentUser(currentUserId, "admin@techodist.local"),
            NullLogger<UpdateAdminUserCommandHandler>.Instance);

    [Fact]
    public async Task Handle_RemovingOwnAdminRole_ReturnsConflict()
    {
        var adminId = Guid.NewGuid();
        var identity = new FakeIdentityService();

        var result = await CreateHandler(identity, adminId).Handle(
            new UpdateAdminUserCommand(adminId, "Администратор магазина", ["Manager"]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.user.cannot_remove_own_admin_role", result.Error.Code);
        Assert.Null(identity.LastUpdated);
    }

    [Fact]
    public async Task Handle_KeepingOwnAdminRole_Succeeds()
    {
        var adminId = Guid.NewGuid();
        var identity = new FakeIdentityService();

        var result = await CreateHandler(identity, adminId).Handle(
            new UpdateAdminUserCommand(adminId, "Администратор магазина", ["Admin", "Manager"]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = identity.LastUpdated;
        Assert.NotNull(updated);
        Assert.Equal(adminId, updated!.Value.UserId);
        Assert.Equal(new[] { "Admin", "Manager" }, updated.Value.Roles);
    }

    [Fact]
    public async Task Handle_OtherUser_TrimsDisplayName()
    {
        var targetId = Guid.NewGuid();
        var identity = new FakeIdentityService();

        var result = await CreateHandler(identity).Handle(
            new UpdateAdminUserCommand(targetId, "  Менеджер заявок  ", ["manager"]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var updated = identity.LastUpdated;
        Assert.NotNull(updated);
        Assert.Equal(targetId, updated!.Value.UserId);
        Assert.Equal("Менеджер заявок", updated.Value.DisplayName);
        Assert.Equal(new[] { "Manager" }, updated.Value.Roles);
    }

    [Fact]
    public async Task Handle_UnknownRole_ReturnsValidationError()
    {
        var identity = new FakeIdentityService();

        var result = await CreateHandler(identity).Handle(
            new UpdateAdminUserCommand(Guid.NewGuid(), "Менеджер", ["SuperUser"]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.user.invalid_role", result.Error.Code);
        Assert.Null(identity.LastUpdated);
    }

    [Fact]
    public async Task Handle_NoRoles_ReturnsRolesRequiredError()
    {
        var identity = new FakeIdentityService();

        var result = await CreateHandler(identity).Handle(
            new UpdateAdminUserCommand(Guid.NewGuid(), "Менеджер", []),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.user.roles_required", result.Error.Code);
        Assert.Null(identity.LastUpdated);
    }
}

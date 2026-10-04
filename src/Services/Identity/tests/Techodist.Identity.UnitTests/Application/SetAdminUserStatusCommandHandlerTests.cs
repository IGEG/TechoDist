using Microsoft.Extensions.Logging.Abstractions;
using Techodist.Identity.Application.Features.Users.Commands.SetAdminUserStatus;
using Techodist.Identity.UnitTests.Fakes;
using Xunit;

namespace Techodist.Identity.UnitTests.Application;

public sealed class SetAdminUserStatusCommandHandlerTests
{
    private static SetAdminUserStatusCommandHandler CreateHandler(
        FakeIdentityService identity,
        Guid? currentUserId = null)
        => new(
            identity,
            new FakeCurrentUser(currentUserId, "admin@techodist.local"),
            NullLogger<SetAdminUserStatusCommandHandler>.Instance);

    [Fact]
    public async Task Handle_DisablingSelf_ReturnsConflict()
    {
        var adminId = Guid.NewGuid();
        var identity = new FakeIdentityService();

        var result = await CreateHandler(identity, adminId).Handle(
            new SetAdminUserStatusCommand(adminId, IsActive: false),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.user.cannot_disable_self", result.Error.Code);
        Assert.Null(identity.LastStatusChange);
    }

    [Fact]
    public async Task Handle_EnablingSelf_Succeeds()
    {
        var adminId = Guid.NewGuid();
        var identity = new FakeIdentityService();

        var result = await CreateHandler(identity, adminId).Handle(
            new SetAdminUserStatusCommand(adminId, IsActive: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var status = identity.LastStatusChange;
        Assert.NotNull(status);
        Assert.Equal(adminId, status!.Value.UserId);
        Assert.True(status.Value.IsActive);
    }

    [Fact]
    public async Task Handle_DisablingAnotherUser_Succeeds()
    {
        var targetId = Guid.NewGuid();
        var identity = new FakeIdentityService();

        var result = await CreateHandler(identity).Handle(
            new SetAdminUserStatusCommand(targetId, IsActive: false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var status = identity.LastStatusChange;
        Assert.NotNull(status);
        Assert.Equal(targetId, status!.Value.UserId);
        Assert.False(status.Value.IsActive);
    }
}

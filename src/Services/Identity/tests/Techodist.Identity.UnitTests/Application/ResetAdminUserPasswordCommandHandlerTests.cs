using Microsoft.Extensions.Logging.Abstractions;
using Techodist.Identity.Application.Features.Users.Commands.ResetAdminUserPassword;
using Techodist.Identity.UnitTests.Fakes;
using Xunit;

namespace Techodist.Identity.UnitTests.Application;

public sealed class ResetAdminUserPasswordCommandHandlerTests
{
    private static ResetAdminUserPasswordCommandHandler CreateHandler(FakeIdentityService identity)
        => new(identity, NullLogger<ResetAdminUserPasswordCommandHandler>.Instance);

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var identity = new FakeIdentityService();

        var result = await CreateHandler(identity).Handle(
            new ResetAdminUserPasswordCommand(Guid.NewGuid(), "Techodist!Dev2027"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.user.not_found", result.Error.Code);
        Assert.Null(identity.LastPasswordReset);
    }

    [Fact]
    public async Task Handle_WeakNewPassword_ReturnsWeakPasswordError()
    {
        var user = AdminUserTestData.Create();
        var identity = new FakeIdentityService();
        identity.Seed(user);

        var result = await CreateHandler(identity).Handle(
            new ResetAdminUserPasswordCommand(user.Id, "password"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.password.weak", result.Error.Code);
        Assert.Null(identity.LastPasswordReset);
    }

    [Fact]
    public async Task Handle_ValidCommand_ResetsPasswordWithoutCurrentOne()
    {
        var user = AdminUserTestData.Create();
        var identity = new FakeIdentityService();
        identity.Seed(user);

        var result = await CreateHandler(identity).Handle(
            new ResetAdminUserPasswordCommand(user.Id, "Techodist!Dev2027"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var reset = identity.LastPasswordReset;
        Assert.NotNull(reset);
        Assert.Equal(user.Id, reset!.Value.UserId);
        Assert.Equal("Techodist!Dev2027", reset.Value.NewPassword);
    }
}

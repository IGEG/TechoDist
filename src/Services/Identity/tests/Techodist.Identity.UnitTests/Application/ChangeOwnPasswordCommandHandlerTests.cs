using Microsoft.Extensions.Logging.Abstractions;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.Identity.Application.Features.Auth.Commands.ChangeOwnPassword;
using Techodist.Identity.UnitTests.Fakes;
using Xunit;

namespace Techodist.Identity.UnitTests.Application;

public sealed class ChangeOwnPasswordCommandHandlerTests
{
    private static ChangeOwnPasswordCommandHandler CreateHandler(FakeIdentityService identity)
        => new(identity, NullLogger<ChangeOwnPasswordCommandHandler>.Instance);

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var identity = new FakeIdentityService();

        var result = await CreateHandler(identity).Handle(
            new ChangeOwnPasswordCommand(Guid.NewGuid(), "Techodist!Dev2026", "Techodist!Dev2027"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.user.not_found", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Null(identity.LastPasswordChange);
    }

    [Fact]
    public async Task Handle_WeakNewPassword_ReturnsWeakPasswordError()
    {
        var user = AdminUserTestData.Create();
        var identity = new FakeIdentityService();
        identity.Seed(user);

        var result = await CreateHandler(identity).Handle(
            new ChangeOwnPasswordCommand(user.Id, "Techodist!Dev2026", "short"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.password.weak", result.Error.Code);
        Assert.Null(identity.LastPasswordChange);
    }

    [Fact]
    public async Task Handle_NewPasswordEqualToEmail_ReturnsWeakPasswordError()
    {
        var user = AdminUserTestData.Create();
        var identity = new FakeIdentityService();
        identity.Seed(user);

        var result = await CreateHandler(identity).Handle(
            new ChangeOwnPasswordCommand(user.Id, "Techodist!Dev2026", AdminUserTestData.Email),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.password.weak", result.Error.Code);
        Assert.Null(identity.LastPasswordChange);
    }

    [Fact]
    public async Task Handle_ValidCommand_ChangesPassword()
    {
        var user = AdminUserTestData.Create();
        var identity = new FakeIdentityService();
        identity.Seed(user);

        var result = await CreateHandler(identity).Handle(
            new ChangeOwnPasswordCommand(user.Id, "Techodist!Dev2026", "Techodist!Dev2027"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var change = identity.LastPasswordChange;
        Assert.NotNull(change);
        Assert.Equal(user.Id, change!.Value.UserId);
        Assert.Equal("Techodist!Dev2026", change.Value.CurrentPassword);
        Assert.Equal("Techodist!Dev2027", change.Value.NewPassword);
    }

    [Fact]
    public async Task Handle_WrongCurrentPassword_PropagatesServiceError()
    {
        var user = AdminUserTestData.Create();
        var identity = new FakeIdentityService
        {
            Failure = Error.Validation("identity.password.mismatch", "Текущий пароль указан неверно."),
        };
        identity.Seed(user);

        var result = await CreateHandler(identity).Handle(
            new ChangeOwnPasswordCommand(user.Id, "Wrong!Dev2026", "Techodist!Dev2027"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.password.mismatch", result.Error.Code);
    }
}

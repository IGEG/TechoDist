using Microsoft.Extensions.Logging.Abstractions;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.Identity.Application.Dtos;
using Techodist.Identity.Application.Features.Auth.Commands.AuthenticateAdmin;
using Techodist.Identity.UnitTests.Fakes;
using Xunit;

namespace Techodist.Identity.UnitTests.Application;

public sealed class AuthenticateAdminCommandHandlerTests
{
    private static AuthenticateAdminCommandHandler CreateHandler(FakeIdentityService identity)
        => new(identity, NullLogger<AuthenticateAdminCommandHandler>.Instance);

    [Fact]
    public async Task Handle_Success_ReturnsAuthenticatedAdmin()
    {
        var adminId = Guid.NewGuid();
        var identity = new FakeIdentityService
        {
            AuthenticatedAdmin = new AuthenticatedAdminDto(
                adminId,
                "admin@techodist.local",
                "Администратор магазина",
                ["Admin"]),
        };

        var result = await CreateHandler(identity).Handle(
            new AuthenticateAdminCommand("admin@techodist.local", "Techodist!Dev2026"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(adminId, result.Value.Id);
        Assert.Equal(new[] { "Admin" }, result.Value.Roles);

        var credentials = identity.LastCredentials;
        Assert.NotNull(credentials);
        Assert.Equal("admin@techodist.local", credentials!.Value.Email);
        Assert.Equal("Techodist!Dev2026", credentials.Value.Password);
    }

    [Fact]
    public async Task Handle_WrongCredentials_PropagatesUnauthorizedError()
    {
        var identity = new FakeIdentityService
        {
            Failure = Error.Unauthorized("identity.invalid_credentials", "Неверный e-mail или пароль."),
        };

        var result = await CreateHandler(identity).Handle(
            new AuthenticateAdminCommand("admin@techodist.local", "wrong-password"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.invalid_credentials", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    [Fact]
    public async Task Handle_LockedAccount_PropagatesLockError()
    {
        var identity = new FakeIdentityService
        {
            Failure = Error.Unauthorized("identity.account_locked", "Учётная запись заблокирована."),
        };

        var result = await CreateHandler(identity).Handle(
            new AuthenticateAdminCommand("admin@techodist.local", "Techodist!Dev2026"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.account_locked", result.Error.Code);
    }
}

using Techodist.Identity.Application.Features.Users.Queries.GetAdminProfile;
using Techodist.Identity.UnitTests.Fakes;
using Xunit;

namespace Techodist.Identity.UnitTests.Application;

public sealed class GetAdminProfileQueryHandlerTests
{
    [Fact]
    public async Task Handle_ExistingUser_ReturnsProfile()
    {
        var user = AdminUserTestData.Create(roles: ["Admin", "Manager"]);
        var identity = new FakeIdentityService();
        identity.Seed(user);

        var result = await new GetAdminProfileQueryHandler(identity)
            .Handle(new GetAdminProfileQuery(user.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value.Id);
        Assert.Equal(AdminUserTestData.Email, result.Value.Email);
        Assert.Equal(new[] { "Admin", "Manager" }, result.Value.Roles);
    }

    [Fact]
    public async Task Handle_UnknownUser_ReturnsNotFound()
    {
        var identity = new FakeIdentityService();

        var result = await new GetAdminProfileQueryHandler(identity)
            .Handle(new GetAdminProfileQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("identity.user.not_found", result.Error.Code);
    }
}

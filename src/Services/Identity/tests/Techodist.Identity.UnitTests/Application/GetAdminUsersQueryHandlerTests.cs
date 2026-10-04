using Techodist.Identity.Application.Features.Users.Queries.GetAdminUsers;
using Techodist.Identity.UnitTests.Fakes;
using Xunit;

namespace Techodist.Identity.UnitTests.Application;

public sealed class GetAdminUsersQueryHandlerTests
{
    [Fact]
    public async Task Handle_NormalizesPaging_AndPassesSearch()
    {
        var identity = new FakeIdentityService();
        identity.Seed(
            AdminUserTestData.Create(email: "admin@techodist.local"),
            AdminUserTestData.Create(email: "manager@techodist.local"));

        var result = await new GetAdminUsersQueryHandler(identity).Handle(
            new GetAdminUsersQuery(Page: 0, PageSize: 500, Search: "techodist"),
            CancellationToken.None);

        var query = identity.LastListQuery;
        Assert.NotNull(query);
        Assert.Equal(1, query!.Value.Page);
        Assert.Equal(100, query.Value.PageSize);
        Assert.Equal("techodist", query.Value.Search);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-5, 20)]
    [InlineData(3, 3)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    public void NormalizedPageSize_ClampsToAllowedRange(int pageSize, int expected)
        => Assert.Equal(expected, new GetAdminUsersQuery(PageSize: pageSize).NormalizedPageSize);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-10, 1)]
    [InlineData(7, 7)]
    public void NormalizedPage_IsAtLeastOne(int page, int expected)
        => Assert.Equal(expected, new GetAdminUsersQuery(Page: page).NormalizedPage);
}

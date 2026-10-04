using Techodist.Identity.Domain.Security;
using Xunit;

namespace Techodist.Identity.UnitTests.Domain;

public sealed class AdminRolesTests
{
    [Theory]
    [InlineData("Admin")]
    [InlineData("admin")]
    [InlineData(" Manager ")]
    public void IsKnown_AcceptsCanonicalRoles_IgnoringCaseAndSpaces(string role)
        => Assert.True(AdminRoles.IsKnown(role));

    [Theory]
    [InlineData("Client")]
    [InlineData("Administrator")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsKnown_RejectsUnknownRoles(string? role)
        => Assert.False(AdminRoles.IsKnown(role));

    [Fact]
    public void Normalize_ReturnsCanonicalOrder_DroppingUnknownAndDuplicates()
    {
        var roles = AdminRoles.Normalize(["manager", "ADMIN", "admin", "Client", " "]);

        Assert.Equal(new[] { "Admin", "Manager" }, roles);
    }

    [Fact]
    public void Normalize_EmptyInput_ReturnsEmptyList()
    {
        Assert.Empty(AdminRoles.Normalize(null));
        Assert.Empty(AdminRoles.Normalize([]));
    }

    [Fact]
    public void All_ContainsAdminAndManagerOnly()
        => Assert.Equal(new[] { "Admin", "Manager" }, AdminRoles.All);
}

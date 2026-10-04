using Techodist.Identity.Domain.ValueObjects;
using Xunit;

namespace Techodist.Identity.UnitTests.Domain;

public sealed class EmailAddressTests
{
    [Fact]
    public void Create_TrimsAndLowercases()
    {
        var email = EmailAddress.Create("  Admin@Techodist.Local ");

        Assert.Equal("admin@techodist.local", email.Value);
        Assert.Equal("admin@techodist.local", (string)email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("@techodist.local")]
    [InlineData("admin@techodist")]
    [InlineData("admin@techodist.")]
    [InlineData("admin @techodist.local")]
    public void TryCreate_RejectsInvalidValues(string? value)
    {
        Assert.False(EmailAddress.TryCreate(value, out var email));
        Assert.Null(email);
    }

    [Fact]
    public void TryCreate_RejectsValueLongerThanMaxLength()
    {
        var value = $"{new string('a', EmailAddress.MaxLength)}@techodist.local";

        Assert.False(EmailAddress.TryCreate(value, out _));
    }

    [Fact]
    public void Create_InvalidValue_Throws()
        => Assert.Throws<ArgumentException>(() => EmailAddress.Create("invalid"));

    [Fact]
    public void Equality_IgnoresCaseAndSurroundingSpaces()
    {
        Assert.Equal(EmailAddress.Create("Admin@Techodist.Local"), EmailAddress.Create("admin@techodist.local"));
        Assert.NotEqual(EmailAddress.Create("admin@techodist.local"), EmailAddress.Create("manager@techodist.local"));
    }
}

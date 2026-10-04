using EcoTech.Catalog.Domain.ValueObjects;
using Xunit;

namespace EcoTech.Catalog.UnitTests.Domain;

public sealed class SlugTests
{
    [Fact]
    public void FromName_TransliteratesCyrillic_AndBuildsSlug()
    {
        var slug = Slug.FromName("Установка ЭКОТЕХ ET-60");

        Assert.Equal("ustanovka-ekoteh-et-60", slug.Value);
    }

    [Fact]
    public void FromName_CollapsesRepeatedSeparators()
    {
        var slug = Slug.FromName("  Вакуумная   установка  ");

        Assert.Equal("vakuumnaya-ustanovka", slug.Value);
    }

    [Fact]
    public void Create_NormalizesInput()
    {
        var slug = Slug.Create("  My-Product--Name  ");

        Assert.Equal("my-product-name", slug.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    [InlineData("invalid_slug")]
    public void Create_InvalidValue_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => Slug.Create(value));
    }

    [Fact]
    public void Equals_ComparesByValue()
    {
        var left = Slug.Create("et-60");
        var right = Slug.FromName("ET-60");

        Assert.Equal(left, right);
        Assert.True(left == right);
    }
}

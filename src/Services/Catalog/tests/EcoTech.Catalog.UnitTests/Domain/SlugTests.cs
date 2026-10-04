using EcoTech.Catalog.Domain.ValueObjects;
using Xunit;

namespace EcoTech.Catalog.UnitTests.Domain;

public sealed class SlugTests
{
    [Fact]
    public void FromName_TransliteratesCyrillic_AndBuildsSlug()
    {
        var slug = Slug.FromName("Установка регенерации TD60");

        Assert.Equal("ustanovka-regeneratsii-td60", slug.Value);
    }

    [Fact]
    public void FromName_MixedBrandAndModel_ProducesSeoFriendlySlug()
    {
        var slug = Slug.FromName("Установка регенерации растворителей Techodist TD20");

        Assert.Equal("ustanovka-regeneratsii-rastvoriteley-techodist-td20", slug.Value);
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
        var left = Slug.Create("td60");
        var right = Slug.FromName("TD60");

        Assert.Equal(left, right);
        Assert.True(left == right);
    }
}

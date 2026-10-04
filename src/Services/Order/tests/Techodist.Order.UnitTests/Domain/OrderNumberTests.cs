using Techodist.Order.Domain.ValueObjects;
using Techodist.Order.UnitTests.Fakes;
using Xunit;

namespace Techodist.Order.UnitTests.Domain;

/// <summary>Читаемый номер заявки: гость диктует его менеджеру по телефону.</summary>
public sealed class OrderNumberTests
{
    [Fact]
    public void Create_FormatsPrefixDateAndPaddedSequence()
    {
        var number = OrderNumber.Create(new DateOnly(2026, 4, 10), 42);

        Assert.Equal("TD-20260410-00042", number.Value);
        Assert.Equal("TD-20260410-00042", number.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_NonPositiveSequence_IsRejected(long sequence)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OrderNumber.Create(OrderTestData.DefaultDate, sequence));
    }

    [Theory]
    [InlineData("TD-20260410-00001")]
    [InlineData("  td-20260410-00001  ")]
    [InlineData("TD-20260410-123456")]
    public void Parse_ValidNumber_ReturnsNormalizedValue(string raw)
    {
        var result = OrderNumber.Parse(raw);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderNumber.Prefix, result.Value.Value[..2]);
        Assert.Equal(result.Value.Value.ToUpperInvariant(), result.Value.Value);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("XX-20260410-00001")]
    [InlineData("TD-2026-00001")]
    [InlineData("TD-2026041X-00001")]
    [InlineData("TD-20260410-1")]
    [InlineData("TD-20260410-00001-2")]
    public void Parse_MalformedNumber_IsValidationError(string raw)
    {
        var result = OrderNumber.Parse(raw);

        Assert.True(result.IsFailure);
        Assert.Equal("order.number.invalid", result.Error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_MissingNumber_ReportsRequiredCode(string? raw)
    {
        var result = OrderNumber.Parse(raw);

        Assert.True(result.IsFailure);
        Assert.Equal("order.number.required", result.Error.Code);
    }

    [Fact]
    public void Equality_IsByValue()
    {
        var first = OrderNumber.Create(OrderTestData.DefaultDate, 7);
        var second = OrderNumber.Parse("td-20260410-00007").Value;

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, OrderNumber.Create(OrderTestData.DefaultDate, 8));
    }
}

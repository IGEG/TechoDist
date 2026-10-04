using Techodist.Basket.Domain.ValueObjects;
using Xunit;

namespace Techodist.Basket.UnitTests.Domain;

public sealed class MoneyTests
{
    [Fact]
    public void Create_RoundsToTwoDecimals_AwayFromZero()
    {
        var money = Money.Create(100.005m);

        Assert.Equal(100.01m, money.Amount);
    }

    [Fact]
    public void Rub_SetsDefaultCurrency()
    {
        var money = Money.Rub(485_000m);

        Assert.Equal(485_000m, money.Amount);
        Assert.Equal("RUB", money.Currency);
    }

    [Fact]
    public void Create_NegativeAmount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Create(-1m));
    }

    [Fact]
    public void Create_BlankCurrency_Throws()
    {
        Assert.Throws<ArgumentException>(() => Money.Create(100m, " "));
    }

    [Fact]
    public void Multiply_ReturnsLineTotal()
    {
        var money = Money.Rub(99_990m);

        Assert.Equal(299_970m, money.Multiply(3).Amount);
    }

    [Fact]
    public void Multiply_NegativeQuantity_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Rub(100m).Multiply(-1));
    }

    [Fact]
    public void Equals_ComparesByAmountAndCurrency()
    {
        Assert.Equal(Money.Rub(1000m), Money.Create(1000m, "rub"));
        Assert.NotEqual(Money.Rub(1000m), Money.Create(1000m, "USD"));
    }
}

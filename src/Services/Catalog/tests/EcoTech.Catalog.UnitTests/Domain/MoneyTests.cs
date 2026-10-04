using EcoTech.Catalog.Domain.ValueObjects;
using Xunit;

namespace EcoTech.Catalog.UnitTests.Domain;

public sealed class MoneyTests
{
    [Fact]
    public void Create_RoundsToTwoDecimals_AwayFromZero()
    {
        var money = Money.Create(100.005m);

        Assert.Equal(100.01m, money.Amount);
    }

    [Fact]
    public void Rub_SetsCurrency()
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
    public void Equals_ComparesByAmountAndCurrency()
    {
        Assert.Equal(Money.Rub(1000m), Money.Create(1000m, "RUB"));
        Assert.NotEqual(Money.Rub(1000m), Money.Create(1000m, "USD"));
    }
}

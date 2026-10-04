using System.Globalization;
using Techodist.Order.Domain.ValueObjects;
using Xunit;

namespace Techodist.Order.UnitTests.Domain;

/// <summary>Сумма с валютой: округление до копеек и запрет отрицательных значений.</summary>
public sealed class MoneyTests
{
    [Fact]
    public void Create_RoundsToTwoDecimalsAwayFromZero()
    {
        // Копейки — минимальная единица: суммы округляются до двух знаков.
        Assert.Equal(100.01m, Money.Rub(100.005m).Amount);
        Assert.Equal(100.02m, Money.Rub(100.015m).Amount);
        Assert.Equal(99.99m, Money.Rub(99.994m).Amount);
        Assert.Equal(485_000.00m, Money.Rub(485_000m).Amount);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    public void Create_NegativeAmount_IsRejected(decimal amount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Money.Rub(amount));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankCurrency_IsRejected(string currency)
    {
        Assert.Throws<ArgumentException>(() => Money.Create(10m, currency));
    }

    [Fact]
    public void Create_NormalizesCurrency()
    {
        var money = Money.Create(10m, " rub ");

        Assert.Equal("RUB", money.Currency);
        Assert.Equal(Money.DefaultCurrency, money.Currency);
    }

    [Fact]
    public void Equality_IsByAmountAndCurrency()
    {
        Assert.Equal(Money.Create(485_000m, "rub"), Money.Rub(485_000m));
        Assert.NotEqual(Money.Rub(485_000m), Money.Create(485_000m, "USD"));
        Assert.NotEqual(Money.Rub(485_000m), Money.Rub(485_001m));
    }

    [Fact]
    public void ToString_ShowsAmountAndCurrency()
    {
        // Формат суммы фиксированный («0.00»), поэтому проверяем его в инвариантной культуре:
        // иначе тест «сломается» на машине с локалью, где разделитель — запятая.
        using var culture = new CultureScope(CultureInfo.InvariantCulture);

        Assert.Equal("48500.00 RUB", Money.Rub(48_500m).ToString());
    }

    /// <summary>Временно подменяет культуру потока, чтобы проверять форматирование.</summary>
    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _original = CultureInfo.CurrentCulture;

        public CultureScope(CultureInfo culture) => CultureInfo.CurrentCulture = culture;

        public void Dispose() => CultureInfo.CurrentCulture = _original;
    }
}

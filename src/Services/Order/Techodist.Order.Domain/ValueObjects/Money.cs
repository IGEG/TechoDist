using Techodist.BuildingBlocks.Core.Entities;

namespace Techodist.Order.Domain.ValueObjects;

/// <summary>
/// Сумма с валютой. Своя копия value object: Order не ссылается ни на Catalog, ни на Basket —
/// границы сервисов (ADR 0001), обмен только через API и события.
/// </summary>
public sealed class Money : ValueObject
{
    /// <summary>Валюта магазина по умолчанию.</summary>
    public const string DefaultCurrency = "RUB";

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Create(decimal amount, string currency = DefaultCurrency)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Сумма не может быть отрицательной.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Валюта обязательна.", nameof(currency));
        }

        return new Money(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), currency.Trim().ToUpperInvariant());
    }

    public static Money Rub(decimal amount) => Create(amount, DefaultCurrency);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}

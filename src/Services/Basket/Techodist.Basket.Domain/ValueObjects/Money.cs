using Techodist.BuildingBlocks.Core.Entities;

namespace Techodist.Basket.Domain.ValueObjects;

/// <summary>
/// Снимок цены на момент добавления товара в корзину: сумма и валюта.
/// Своя копия value object: Basket не ссылается на Catalog (границы сервисов, ADR 0001).
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

    /// <summary>Стоимость нескольких единиц товара (цена × количество).</summary>
    public Money Multiply(int quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Количество не может быть отрицательным.");
        }

        return Create(Amount * quantity, Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}

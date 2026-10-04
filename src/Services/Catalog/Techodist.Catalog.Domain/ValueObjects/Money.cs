using Techodist.BuildingBlocks.Core.Entities;

namespace Techodist.Catalog.Domain.ValueObjects;

/// <summary>Денежная сумма с валютой (по умолчанию RUB).</summary>
public sealed class Money : ValueObject
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Create(decimal amount, string currency = "RUB")
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

    public static Money Rub(decimal amount) => Create(amount, "RUB");

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}

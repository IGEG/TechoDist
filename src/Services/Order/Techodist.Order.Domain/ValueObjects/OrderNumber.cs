using System.Globalization;
using Techodist.BuildingBlocks.Core.Entities;
using Techodist.BuildingBlocks.Core.Results;

namespace Techodist.Order.Domain.ValueObjects;

/// <summary>
/// Номер заявки: читаемый человеком идентификатор вида <c>TD-20261004-00042</c>.
/// Внутренний <c>Id</c> остаётся GUID (ссылки в API), а номер клиент называет менеджеру
/// по телефону — и по нему же получает статус заявки.
/// </summary>
public sealed class OrderNumber : ValueObject
{
    /// <summary>Префикс торговой марки Techodist.</summary>
    public const string Prefix = "TD";

    private const int SequenceDigits = 5;

    private OrderNumber(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Номер из даты оформления и сквозного номера дня. Сквозной номер выдаёт БД
    /// (последовательность PostgreSQL), поэтому два заказа не получат один номер.
    /// </summary>
    public static OrderNumber Create(DateOnly date, long sequence)
    {
        if (sequence < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Сквозной номер заявки должен быть положительным.");
        }

        var day = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);

        return new OrderNumber($"{Prefix}-{day}-{sequence.ToString($"D{SequenceDigits}", CultureInfo.InvariantCulture)}");
    }

    /// <summary>Разбор номера из запроса (клиент ищет заявку по номеру из письма).</summary>
    public static Result<OrderNumber> Parse(string? raw)
    {
        var normalized = raw?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Result.Failure<OrderNumber>(
                Error.Validation("order.number.required", "Номер заявки обязателен."));
        }

        var parts = normalized.Split('-');

        var isValid =
            parts.Length == 3 &&
            parts[0] == Prefix &&
            parts[1].Length == 8 &&
            parts[1].All(char.IsAsciiDigit) &&
            parts[2].Length >= SequenceDigits &&
            parts[2].All(char.IsAsciiDigit);

        return isValid
            ? Result.Success(new OrderNumber(normalized))
            : Result.Failure<OrderNumber>(Error.Validation(
                "order.number.invalid",
                $"Номер заявки должен иметь вид {Prefix}-ГГГГММДД-00001."));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}

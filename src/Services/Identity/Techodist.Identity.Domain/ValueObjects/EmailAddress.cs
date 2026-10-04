using System.Text.RegularExpressions;
using Techodist.BuildingBlocks.Core.Entities;

namespace Techodist.Identity.Domain.ValueObjects;

/// <summary>
/// E-mail администратора. Value object: хранит нормализованное значение (trim + нижний регистр),
/// поэтому уникальность логина в БД обеспечивается без дополнительных проверок регистра.
/// </summary>
public sealed partial class EmailAddress : ValueObject
{
    /// <summary>Максимальная длина e-mail по RFC 5321.</summary>
    public const int MaxLength = 254;

    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static EmailAddress Create(string? value)
    {
        if (!TryCreate(value, out var email))
        {
            throw new ArgumentException($"Некорректный e-mail: '{value}'.", nameof(value));
        }

        return email!;
    }

    public static bool TryCreate(string? value, out EmailAddress? email)
    {
        email = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength || !EmailPattern().IsMatch(normalized))
        {
            return false;
        }

        email = new EmailAddress(normalized);
        return true;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(EmailAddress email) => email.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();
}

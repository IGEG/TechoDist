using System.Globalization;
using System.Text;
using EcoTech.BuildingBlocks.Core.Entities;

namespace EcoTech.Catalog.Domain.ValueObjects;

/// <summary>
/// Человекочитаемый URL-идентификатор (например, "ustanovka-techodist-td60").
/// Поддерживает транслитерацию кириллицы в латиницу для SEO-адресов.
/// </summary>
public sealed class Slug : ValueObject
{
    private static readonly Dictionary<char, string> TransliterationTable = new()
    {
        ['а'] = "a", ['б'] = "b", ['в'] = "v", ['г'] = "g", ['д'] = "d", ['е'] = "e",
        ['ё'] = "e", ['ж'] = "zh", ['з'] = "z", ['и'] = "i", ['й'] = "y", ['к'] = "k",
        ['л'] = "l", ['м'] = "m", ['н'] = "n", ['о'] = "o", ['п'] = "p", ['р'] = "r",
        ['с'] = "s", ['т'] = "t", ['у'] = "u", ['ф'] = "f", ['х'] = "h", ['ц'] = "ts",
        ['ч'] = "ch", ['ш'] = "sh", ['щ'] = "sch", ['ъ'] = string.Empty, ['ы'] = "y",
        ['ь'] = string.Empty, ['э'] = "e", ['ю'] = "yu", ['я'] = "ya",
    };

    private Slug(string value) => Value = value;

    public string Value { get; }

    /// <summary>Создаёт slug из произвольного названия (с транслитерацией).</summary>
    public static Slug FromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Название для slug обязательно.", nameof(name));
        }

        var builder = new StringBuilder(name.Length);

        foreach (var ch in name.Trim().ToLowerInvariant())
        {
            if (TransliterationTable.TryGetValue(ch, out var transliterated))
            {
                builder.Append(transliterated);
            }
            else if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else
            {
                builder.Append('-');
            }
        }

        var slug = CollapseDashes(builder.ToString());

        if (slug.Length == 0)
        {
            throw new InvalidOperationException($"Не удалось сгенерировать slug из '{name}'.");
        }

        return new Slug(slug);
    }

    public static Slug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Slug обязателен.", nameof(value));
        }

        var normalized = CollapseDashes(value.Trim().ToLowerInvariant());

        if (normalized.Length == 0 || !normalized.All(c => char.IsLetterOrDigit(c) || c == '-'))
        {
            throw new ArgumentException($"Некорректный slug '{value}'.", nameof(value));
        }

        return new Slug(normalized);
    }

    private static string CollapseDashes(string input)
    {
        var parts = input.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join('-', parts);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Slug slug) => slug.Value;
}

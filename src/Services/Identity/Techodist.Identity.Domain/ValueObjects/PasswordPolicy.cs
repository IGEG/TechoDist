namespace Techodist.Identity.Domain.ValueObjects;

/// <summary>
/// Политика паролей администраторов — единый источник правил для домена,
/// валидаторов приложения и настроек ASP.NET Core Identity.
/// </summary>
public sealed class PasswordPolicy
{
    /// <summary>Минимальная длина пароля администратора.</summary>
    public const int MinimumLength = 12;

    /// <summary>Политика по умолчанию (используется API и Identity).</summary>
    public static readonly PasswordPolicy Default = new();

    /// <summary>Проверяет пароль и возвращает список нарушений (пустой список — пароль принят).</summary>
    public IReadOnlyList<string> Validate(string? password, string? email = null)
    {
        var violations = new List<string>();

        if (string.IsNullOrWhiteSpace(password))
        {
            violations.Add("Пароль обязателен.");
            return violations;
        }

        if (password.Length < MinimumLength)
        {
            violations.Add($"Пароль должен содержать минимум {MinimumLength} символов.");
        }

        if (!password.Any(char.IsUpper))
        {
            violations.Add("Пароль должен содержать заглавную букву.");
        }

        if (!password.Any(char.IsLower))
        {
            violations.Add("Пароль должен содержать строчную букву.");
        }

        if (!password.Any(char.IsDigit))
        {
            violations.Add("Пароль должен содержать цифру.");
        }

        if (password.All(char.IsLetterOrDigit))
        {
            violations.Add("Пароль должен содержать специальный символ.");
        }

        if (!string.IsNullOrWhiteSpace(email)
            && string.Equals(password.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            violations.Add("Пароль не должен совпадать с e-mail.");
        }

        return violations;
    }

    public bool IsSatisfied(string? password, string? email = null) => Validate(password, email).Count == 0;

    /// <summary>Человекочитаемое описание требований (для сообщений об ошибках и UI).</summary>
    public string Describe()
        => $"Минимум {MinimumLength} символов, заглавная и строчная буквы, цифра и специальный символ.";
}

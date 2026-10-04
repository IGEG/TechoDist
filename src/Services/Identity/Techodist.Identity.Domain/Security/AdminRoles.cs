namespace Techodist.Identity.Domain.Security;

/// <summary>
/// Роли админ-панели. По ADR 0004 клиентских учёток нет — регистрация покупателей не предусмотрена,
/// поэтому ролей ровно две: <see cref="Admin"/> (полный доступ) и <see cref="Manager"/> (обработка заявок).
/// </summary>
public static class AdminRoles
{
    public const string Admin = "Admin";

    public const string Manager = "Manager";

    /// <summary>Все допустимые роли в каноническом написании.</summary>
    public static readonly IReadOnlyList<string> All = [Admin, Manager];

    public static bool IsKnown(string? role)
        => !string.IsNullOrWhiteSpace(role)
           && All.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Приводит набор ролей к каноническому виду: убирает неизвестные роли и дубликаты
    /// (с учётом регистра), сохраняя порядок <see cref="All"/>.
    /// </summary>
    public static IReadOnlyList<string> Normalize(IEnumerable<string>? roles)
    {
        if (roles is null)
        {
            return [];
        }

        var requested = roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return [.. All.Where(requested.Contains)];
    }
}

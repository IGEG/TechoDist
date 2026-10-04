using Microsoft.AspNetCore.Identity;

namespace Techodist.Identity.Infrastructure.Persistence;

/// <summary>
/// Учётная запись администратора магазина (ASP.NET Core Identity).
/// Дополнительно к базовым полям Identity храним отображаемое имя, признак активности
/// (мягкая блокировка вместо удаления) и время последнего входа.
/// </summary>
public sealed class AdminUser : IdentityUser<Guid>
{
    /// <summary>Отображаемое имя (например, «Иванов И. И.»).</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Учётка активна. Отключённые учётки не могут получить токены.</summary>
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastLoginAt { get; set; }
}

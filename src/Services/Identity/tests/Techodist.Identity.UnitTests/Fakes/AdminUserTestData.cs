using Techodist.Identity.Application.Dtos;

namespace Techodist.Identity.UnitTests.Fakes;

/// <summary>Фабрика тестовых администраторов — единые значения по умолчанию, чтобы тесты читались короче.</summary>
internal static class AdminUserTestData
{
    public const string Email = "admin@techodist.local";

    public const string DisplayName = "Администратор магазина";

    public static AdminUserDto Create(
        Guid? id = null,
        string email = Email,
        string displayName = DisplayName,
        IReadOnlyList<string>? roles = null,
        bool isActive = true,
        DateTimeOffset? createdAt = null,
        DateTimeOffset? lastLoginAt = null,
        bool isLockedOut = false)
        => new(
            id ?? Guid.NewGuid(),
            email,
            displayName,
            roles ?? ["Admin"],
            isActive,
            createdAt ?? DateTimeOffset.UtcNow,
            lastLoginAt,
            isLockedOut);
}

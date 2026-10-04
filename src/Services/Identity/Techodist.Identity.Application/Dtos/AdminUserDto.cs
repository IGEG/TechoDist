namespace Techodist.Identity.Application.Dtos;

/// <summary>Данные администратора для чтения (без секретов и хэшей).</summary>
public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    bool IsLockedOut);

/// <summary>Данные администратора, подтверждённые паролем (используются для выпуска токенов).</summary>
public sealed record AuthenticatedAdminDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles);

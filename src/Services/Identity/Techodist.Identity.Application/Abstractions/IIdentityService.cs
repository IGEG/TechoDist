using Techodist.BuildingBlocks.Core.Pagination;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.Identity.Application.Dtos;

namespace Techodist.Identity.Application.Abstractions;

/// <summary>
/// Доступ к учётным записям администраторов. Реализуется поверх ASP.NET Core Identity
/// (хэширование паролей, блокировка после неудачных попыток, роли) — как <c>IProductRepository</c> в Catalog,
/// это «порт» приложения, отделяющий CQRS-сценарии от инфраструктуры.
/// </summary>
public interface IIdentityService
{
    /// <summary>Проверяет e-mail и пароль. Учитывает блокировку и отключённые учётки.</summary>
    Task<Result<AuthenticatedAdminDto>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>Смена пароля с проверкой текущего (для самого администратора).</summary>
    Task<Result> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);

    /// <summary>Сброс пароля без проверки текущего (действие администратора админ-панели).</summary>
    Task<Result> ResetPasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken cancellationToken = default);

    Task<AdminUserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PagedResult<AdminUserDto>> ListAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);

    Task<Result<Guid>> CreateAsync(
        string email,
        string displayName,
        string password,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(
        Guid userId,
        string displayName,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default);

    Task<Result> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
}

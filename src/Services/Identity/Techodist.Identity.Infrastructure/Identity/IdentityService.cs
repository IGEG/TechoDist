using Techodist.BuildingBlocks.Core.Pagination;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.Identity.Application.Abstractions;
using Techodist.Identity.Application.Dtos;
using Techodist.Identity.Domain.Security;
using Techodist.Identity.Domain.ValueObjects;
using Techodist.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Techodist.Identity.Infrastructure.Identity;

/// <summary>
/// Реализация <see cref="IIdentityService"/> поверх ASP.NET Core Identity:
/// хэширование паролей, блокировка после неудачных попыток входа, роли и мягкое отключение учёток.
/// </summary>
internal sealed class IdentityService(
    UserManager<AdminUser> userManager,
    SignInManager<AdminUser> signInManager,
    ILogger<IdentityService> logger) : IIdentityService
{
    private static readonly Error InvalidCredentials =
        Error.Unauthorized("identity.invalid_credentials", "Неверный e-mail или пароль.");

    public async Task<Result<AuthenticatedAdminDto>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (!EmailAddress.TryCreate(email, out var address))
        {
            return Result.Failure<AuthenticatedAdminDto>(InvalidCredentials);
        }

        var user = await userManager.FindByEmailAsync(address!.Value);
        if (user is null)
        {
            logger.LogWarning("Неудачная попытка входа: учётная запись {Email} не найдена", address.Value);
            return Result.Failure<AuthenticatedAdminDto>(InvalidCredentials);
        }

        if (!user.IsActive)
        {
            logger.LogWarning("Отклонён вход отключённой учётной записи {Email}", address.Value);
            return Result.Failure<AuthenticatedAdminDto>(
                Error.Unauthorized("identity.account_disabled", "Учётная запись отключена администратором."));
        }

        // lockoutOnFailure: считаем неудачные попытки и блокируем учётку (настройки в AddIdentityCore).
        var signIn = await signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);

        if (signIn.IsLockedOut)
        {
            logger.LogWarning("Учётная запись {Email} заблокирована после неудачных попыток входа", address.Value);
            return Result.Failure<AuthenticatedAdminDto>(
                Error.Unauthorized(
                    "identity.account_locked",
                    "Учётная запись временно заблокирована после нескольких неудачных попыток входа."));
        }

        if (!signIn.Succeeded)
        {
            logger.LogWarning("Неудачная попытка входа: неверный пароль для {Email}", address.Value);
            return Result.Failure<AuthenticatedAdminDto>(InvalidCredentials);
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        var roles = AdminRoles.Normalize(await userManager.GetRolesAsync(user));

        logger.LogInformation("Администратор {Email} вошёл в систему", address.Value);

        return new AuthenticatedAdminDto(user.Id, user.Email ?? address.Value, user.DisplayName, roles);
    }

    public async Task<Result> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(NotFound(userId));
        }

        var result = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(MapIdentityErrors(result, "identity.password.change_failed"));
    }

    public async Task<Result> ResetPasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(NotFound(userId));
        }

        // Сброс через токен: обновляет security stamp и сбрасывает счётчик неудачных попыток.
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);

        if (result.Succeeded)
        {
            await userManager.ResetAccessFailedCountAsync(user);
            await userManager.SetLockoutEndDateAsync(user, null);
        }

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(MapIdentityErrors(result, "identity.password.reset_failed"));
    }

    public async Task<AdminUserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());

        return user is null ? null : await ToDtoAsync(user);
    }

    public async Task<PagedResult<AdminUserDto>> ListAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var query = userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            // ILike — регистронезависимый поиск PostgreSQL; подстановочные знаки пользователя экранируем.
            var pattern = $"%{search.Trim().Replace("%", "\\%", StringComparison.Ordinal)}%";

            query = query.Where(user =>
                EF.Functions.ILike(user.Email!, pattern) || EF.Functions.ILike(user.DisplayName, pattern));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(user => user.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = new List<AdminUserDto>(users.Count);
        foreach (var user in users)
        {
            items.Add(await ToDtoAsync(user));
        }

        return PagedResult<AdminUserDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<Result<Guid>> CreateAsync(
        string email,
        string displayName,
        string password,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return Result.Failure<Guid>(
                Error.Conflict("identity.user.email_exists", $"Администратор с e-mail '{email}' уже существует."));
        }

        var user = new AdminUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            DisplayName = displayName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            return Result.Failure<Guid>(MapIdentityErrors(created, "identity.user.create_failed"));
        }

        var rolesToAssign = AdminRoles.Normalize(roles);
        if (rolesToAssign.Count != 0)
        {
            var assigned = await userManager.AddToRolesAsync(user, rolesToAssign);
            if (!assigned.Succeeded)
            {
                // Откат: не оставляем учётную запись без ролей.
                await userManager.DeleteAsync(user);
                return Result.Failure<Guid>(MapIdentityErrors(assigned, "identity.user.roles_failed"));
            }
        }

        logger.LogInformation("Создан администратор {Email} ({UserId})", email, user.Id);

        return user.Id;
    }

    public async Task<Result> UpdateAsync(
        Guid userId,
        string displayName,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(NotFound(userId));
        }

        var currentRoles = await userManager.GetRolesAsync(user);

        var rolesToRemove = currentRoles
            .Except(roles, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var rolesToAdd = roles
            .Except(currentRoles, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (rolesToRemove.Length != 0)
        {
            var removed = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removed.Succeeded)
            {
                return Result.Failure(MapIdentityErrors(removed, "identity.user.roles_failed"));
            }
        }

        if (rolesToAdd.Length != 0)
        {
            var added = await userManager.AddToRolesAsync(user, rolesToAdd);
            if (!added.Succeeded)
            {
                return Result.Failure(MapIdentityErrors(added, "identity.user.roles_failed"));
            }
        }

        user.DisplayName = displayName;

        var updated = await userManager.UpdateAsync(user);

        return updated.Succeeded
            ? Result.Success()
            : Result.Failure(MapIdentityErrors(updated, "identity.user.update_failed"));
    }

    public async Task<Result> SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure(NotFound(userId));
        }

        user.IsActive = isActive;

        if (isActive)
        {
            // Возвращаем возможность входа: снимаем блокировку после неудачных попыток.
            await userManager.ResetAccessFailedCountAsync(user);
            await userManager.SetLockoutEndDateAsync(user, null);
        }

        var updated = await userManager.UpdateAsync(user);

        return updated.Succeeded
            ? Result.Success()
            : Result.Failure(MapIdentityErrors(updated, "identity.user.update_failed"));
    }

    private async Task<AdminUserDto> ToDtoAsync(AdminUser user)
    {
        var roles = AdminRoles.Normalize(await userManager.GetRolesAsync(user));

        return new AdminUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.DisplayName,
            roles,
            user.IsActive,
            user.CreatedAt,
            user.LastLoginAt,
            user.LockoutEnd is not null && user.LockoutEnd > DateTimeOffset.UtcNow);
    }

    private static Error NotFound(Guid userId)
        => Error.NotFound("identity.user.not_found", $"Администратор '{userId}' не найден.");

    private static Error MapIdentityErrors(IdentityResult result, string code)
        => Error.Validation(
            code,
            string.Join(" ", result.Errors.Select(error => $"{error.Code}: {error.Description}")));
}

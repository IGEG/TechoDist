using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Techodist.Identity.Domain.Security;
using Techodist.Identity.Domain.ValueObjects;
using Techodist.Identity.Infrastructure.Identity;
using Techodist.Identity.Infrastructure.Persistence;

namespace Techodist.Identity.Api;

/// <summary>
/// Подготовка Identity Service при старте: миграции БД, роли админ-панели, первый администратор
/// (bootstrap из конфигурации) и регистрация scope'ов/клиента OpenIddict. Все операции идемпотентны.
/// </summary>
public static class IdentityDataSeeder
{
    public const string AdminEmailKey = "Bootstrap:AdminEmail";
    public const string AdminPasswordKey = "Bootstrap:AdminPassword";
    public const string AdminDisplayNameKey = "Bootstrap:AdminDisplayName";

    private const string DefaultAdminDisplayName = "Администратор";

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        await MigrateAsync(services, logger, cancellationToken);
        await SeedRolesAsync(services, logger);
        await SeedBootstrapAdminAsync(services, configuration, logger);

        // Аудитории API (scope = aud access-токена) и клиент админ-панели.
        await OpenIddictDataSeeder.SeedAsync(services, configuration, cancellationToken);
    }

    private static async Task MigrateAsync(
        IServiceProvider services,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        logger.LogInformation("Применение миграций Identity...");
        await db.Database.MigrateAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(IServiceProvider services, ILogger logger)
    {
        await using var scope = services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var role in AdminRoles.All)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                continue;
            }

            var created = await roleManager.CreateAsync(new IdentityRole<Guid>(role));
            if (!created.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Не удалось создать роль '{role}': {string.Join("; ", created.Errors.Select(error => error.Description))}");
            }

            logger.LogInformation("Создана роль админ-панели {Role}", role);
        }
    }

    /// <summary>
    /// Создаёт первого администратора, если задан <c>Bootstrap:AdminEmail</c> и такого пользователя ещё нет.
    /// Нужен для входа в только что развёрнутую систему: регистрации в админ-панели нет (ADR 0004).
    /// </summary>
    private static async Task SeedBootstrapAdminAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger)
    {
        var email = configuration[AdminEmailKey];

        if (string.IsNullOrWhiteSpace(email))
        {
            logger.LogWarning(
                "Bootstrap-администратор не создан: не задан '{Key}'.",
                AdminEmailKey);
            return;
        }

        if (!EmailAddress.TryCreate(email, out var normalizedEmail))
        {
            throw new InvalidOperationException($"Некорректный '{AdminEmailKey}': '{email}'.");
        }

        await using var scope = services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();

        if (await userManager.FindByEmailAsync(normalizedEmail!.Value) is not null)
        {
            logger.LogInformation("Bootstrap-администратор {Email} уже существует.", normalizedEmail.Value);
            return;
        }

        var password = configuration[AdminPasswordKey];
        var violations = PasswordPolicy.Default.Validate(password, normalizedEmail.Value);
        if (violations.Count != 0)
        {
            throw new InvalidOperationException(
                $"'{AdminPasswordKey}' не соответствует политике паролей: {string.Join(" ", violations)}");
        }

        var admin = new AdminUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedEmail.Value,
            Email = normalizedEmail.Value,
            EmailConfirmed = true,
            DisplayName = configuration[AdminDisplayNameKey] is { Length: > 0 } displayName
                ? displayName
                : DefaultAdminDisplayName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var created = await userManager.CreateAsync(admin, password!);
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                $"Не удалось создать bootstrap-администратора: {string.Join("; ", created.Errors.Select(error => error.Description))}");
        }

        var assigned = await userManager.AddToRoleAsync(admin, AdminRoles.Admin);
        if (!assigned.Succeeded)
        {
            throw new InvalidOperationException(
                $"Не удалось назначить роль Admin: {string.Join("; ", assigned.Errors.Select(error => error.Description))}");
        }

        logger.LogWarning(
            "Создан bootstrap-администратор {Email}. Смените пароль после первого входа.",
            normalizedEmail.Value);
    }
}

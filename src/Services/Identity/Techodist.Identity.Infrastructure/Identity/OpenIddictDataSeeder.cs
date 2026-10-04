using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Techodist.Identity.Infrastructure.Identity;

/// <summary>
/// Идемпотентное сидирование OpenIddict при старте сервиса:
/// регистрируются аудитории API (scope = значение claim <c>aud</c> в access token)
/// и единственный клиент — админ-панель (confidential, потоки password + refresh_token).
/// </summary>
public static class OpenIddictDataSeeder
{
    public const string AdminClientIdConfigKey = "OpenIddict:AdminClientId";
    public const string AdminClientSecretConfigKey = "OpenIddict:AdminClientSecret";

    /// <summary>Идентификатор клиента админ-панели по умолчанию.</summary>
    public const string DefaultAdminClientId = "techodist-admin-panel";

    /// <summary>Аудитория Identity API — её же запрашивает админ-панель для вызова защищённых эндпоинтов.</summary>
    public const string IdentityApiScope = "techodist-identity-api";

    /// <summary>
    /// Аудитории сервисов. Имя scope совпадает с аудиторией, поэтому access token,
    /// запрошенный на нужный scope, проходит проверку аудитории в соответствующем сервисе.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> ApiScopes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [IdentityApiScope] = "Identity API — администраторы админ-панели",
        ["techodist-catalog-api"] = "Catalog API — товары и категории",
        ["techodist-order-api"] = "Order API — заявки на покупку",
    };

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        await using var scope = services.CreateAsyncScope();

        var scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
        var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var (name, displayName) in ApiScopes)
        {
            if (await scopeManager.FindByNameAsync(name, cancellationToken) is null)
            {
                await scopeManager.CreateAsync(
                    new OpenIddictScopeDescriptor
                    {
                        Name = name,
                        DisplayName = displayName,
                        Resources = { name },
                    },
                    cancellationToken);
            }
        }

        var clientId = configuration[AdminClientIdConfigKey] ?? DefaultAdminClientId;

        var clientSecret = configuration[AdminClientSecretConfigKey]
            ?? throw new InvalidOperationException($"Не задан '{AdminClientSecretConfigKey}'.");

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = "Techodist · админ-панель",
            ClientType = ClientTypes.Confidential,
            ConsentType = ConsentTypes.Implicit,
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.Password,
                Permissions.GrantTypes.RefreshToken,
                Permissions.Prefixes.Scope + Scopes.OfflineAccess,
            },
        };

        foreach (var audience in ApiScopes.Keys)
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + audience);
        }

        var existing = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);

        if (existing is null)
        {
            await applicationManager.CreateAsync(descriptor, cancellationToken);
        }
        else
        {
            // Обновляем разрешения/секрет, чтобы изменения в коде применялись при рестарте.
            await applicationManager.UpdateAsync(existing, descriptor, cancellationToken);
        }
    }
}

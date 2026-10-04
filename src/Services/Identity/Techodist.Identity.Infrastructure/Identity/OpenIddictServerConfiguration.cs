using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Techodist.Identity.Infrastructure.Identity;

/// <summary>
/// Настройка OpenIddict Server для админ-панели (ADR 0004):
/// <list type="bullet">
/// <item>поддерживаются только потоки <c>password</c> и <c>refresh_token</c> (регистрации и внешних провайдеров нет);</item>
/// <item>access token — обычный подписанный JWT (<c>DisableAccessTokenEncryption</c>), чтобы его читали другие сервисы;</item>
/// <item>в dev используется симметричный ключ из конфигурации, в prod предусмотрен сертификат.</item>
/// </list>
/// </summary>
internal static class OpenIddictServerConfiguration
{
    /// <summary>Ключ конфигурации с issuer'ом токенов (должен совпадать у всех сервисов).</summary>
    public const string IssuerKey = "Jwt:Issuer";

    /// <summary>Ключ конфигурации с ключом подписи (HS256, минимум 32 байта).</summary>
    public const string SigningKeyKey = "Jwt:SigningKey";

    /// <summary>Ключ конфигурации с ключом шифрования refresh-токенов (AES, минимум 32 байта).</summary>
    public const string EncryptionKeyKey = "Jwt:EncryptionKey";

    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14);

    public static void Configure(
        OpenIddictServerBuilder builder,
        IConfiguration configuration,
        bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        var issuer = configuration[IssuerKey]
            ?? throw new InvalidOperationException($"Не задан '{IssuerKey}'.");

        var signingKey = configuration[SigningKeyKey]
            ?? throw new InvalidOperationException($"Не задан '{SigningKeyKey}'.");

        // Защита от короткого ключа: HS256 требует не менее 256 бит.
        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException($"'{SigningKeyKey}' должен быть не короче 32 символов (256 бит).");
        }

        // Ключ шифрования refresh-токенов; если не задан — берём ключ подписи (dev-упрощение).
        var encryptionKey = configuration[EncryptionKeyKey] ?? signingKey;

        builder
            .SetIssuer(new Uri(issuer))
            // В OpenIddict 4+ endpoint включён только если для него заданы URI,
            // поэтому authorization/device/introspection/revocation/userinfo/endsession остаются выключенными.
            .SetTokenEndpointUris("/connect/token")
            .AllowPasswordFlow()
            .AllowRefreshTokenFlow()
            .SetAccessTokenLifetime(AccessTokenLifetime)
            .SetRefreshTokenLifetime(RefreshTokenLifetime)
            .AddSigningKey(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)))
            .AddEncryptionKey(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(encryptionKey)))
            .DisableAccessTokenEncryption();

        // Обработку запроса на токен (password/refresh) выполняет AuthorizationController.
        var aspNetCore = builder
            .UseAspNetCore()
            .EnableTokenEndpointPassthrough();

        if (isDevelopment)
        {
            // В локальной разработке Identity API слушает HTTP, поэтому требование HTTPS снимаем.
            aspNetCore.DisableTransportSecurityRequirement();
        }
    }
}

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Techodist.BuildingBlocks.Web.Extensions;

/// <summary>
/// Типовое подключение JWT-аутентификации для API-сервисов Techodist.
/// Токены выпускает Identity API (OpenIddict), проверяются они по симметричному ключу
/// <c>Jwt:SigningKey</c>; аудитория сервиса — имя его API (например <c>techodist-catalog-api</c>).
/// Для API Gateway есть перегрузка без проверки аудитории.
/// </summary>
public static class AuthenticationExtensions
{
    public const string IssuerKey = "Jwt:Issuer";
    public const string SigningKeyKey = "Jwt:SigningKey";

    /// <summary>
    /// Роли административного персонала магазина. Строка совпадает с <c>Techodist.Identity.Domain.AdminRoles</c>,
    /// но здесь это обычная константа: Catalog не ссылается на Identity (граница сервисов по ADR 0001).
    /// </summary>
    public const string AdminRoles = "Admin,Manager";

    /// <summary>Роль супер-администратора (совпадает с <c>Techodist.Identity.Domain.AdminRoles.Admin</c>).</summary>
    public const string AdminRole = "Admin";

    /// <summary>
    /// Аутентификация API-сервиса: токен принимается только для аудитории <paramref name="audience"/>.
    /// </summary>
    public static IServiceCollection AddTechodistJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string audience)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);

        return services.AddTechodistJwtAuthentication(configuration, parameters =>
        {
            parameters.ValidateAudience = true;
            parameters.ValidAudience = audience;
        });
    }

    /// <summary>
    /// Аутентификация для шлюза: аудитория не проверяется, потому что один клиент
    /// (админ-панель) вызывает разные API и получает токены с разными аудиториями.
    /// Подпись, issuer и срок жизни проверяются; <c>aud</c> строго проверяет сервис-получатель.
    /// </summary>
    public static IServiceCollection AddTechodistJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
        => services.AddTechodistJwtAuthentication(
            configuration,
            parameters => parameters.ValidateAudience = false);

    private static IServiceCollection AddTechodistJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<TokenValidationParameters> configureValidation)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configureValidation);

        var issuer = configuration[IssuerKey];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException($"Не задан '{IssuerKey}'.");
        }

        var signingKey = configuration[SigningKeyKey];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException($"Не задан '{SigningKeyKey}'.");
        }

        if (Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException($"'{SigningKeyKey}' должен быть не короче 32 символов (256 бит).");
        }

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                var parameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role",
                };

                configureValidation(parameters);

                options.TokenValidationParameters = parameters;
            });

        services.AddAuthorization();

        return services;
    }
}

using Techodist.BuildingBlocks.Web.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Techodist.ApiGateway.Extensions;

/// <summary>
/// Политики авторизации шлюза. Шлюз — «первая линия» защиты: административные маршруты
/// требуют роль, а сервис-получатель проверяет ещё и аудиторию токена (defense in depth).
/// </summary>
public static class AuthorizationExtensions
{
    public static IServiceCollection AddGatewayAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(GatewayPolicies.Admin, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(AuthenticationExtensions.AdminRoles.Split(',')));

            options.AddPolicy(GatewayPolicies.AdminOnly, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(AuthenticationExtensions.AdminRole));
        });

        return services;
    }
}

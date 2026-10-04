namespace Techodist.ApiGateway;

/// <summary>
/// Имена политик шлюза. Строки политик авторизации должны совпадать со значениями
/// <c>AuthorizationPolicy</c> в секции <c>ReverseProxy</c> (appsettings.json),
/// а политик rate-limit — со значениями <c>RateLimiterPolicy</c>.
/// </summary>
public static class GatewayPolicies
{
    /// <summary>Доступ административного персонала: роли <c>Admin</c> или <c>Manager</c>.</summary>
    public const string Admin = "techodist-admin";

    /// <summary>Доступ только для роли <c>Admin</c> (управление администраторами).</summary>
    public const string AdminOnly = "techodist-admin-only";

    /// <summary>Общий лимит запросов к API (скользящее окно на пользователя/IP).</summary>
    public const string GeneralRateLimit = "public-api";

    /// <summary>Жёсткий лимит на token-endpoint: защита от перебора пароля.</summary>
    public const string AuthenticationRateLimit = "auth";

    /// <summary>
    /// Жёсткий лимит на единственную анонимную запись — оформление заявки гостем:
    /// без него форма контактов превращается в открытый релей для спама.
    /// </summary>
    public const string GuestOrderRateLimit = "order-submit";
}

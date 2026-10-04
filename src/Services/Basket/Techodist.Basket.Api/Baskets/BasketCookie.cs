using Microsoft.AspNetCore.Http;

namespace Techodist.Basket.Api.Baskets;

/// <summary>
/// Анонимный идентификатор корзины в cookie (ADR 0005): HttpOnly — JavaScript не читает basketId,
/// Lax — cookie уходит на запросы витрины через шлюз.
/// </summary>
public static class BasketCookie
{
    public const string Name = "techodist_basket";

    /// <summary>
    /// Параметры cookie. <c>Secure</c> выставляется по схеме запроса: в Dev витрина работает по http,
    /// а браузер не сохранит Secure-cookie с http (кроме localhost). В проде (https) cookie закрыта.
    /// </summary>
    public static CookieOptions CreateOptions(HttpRequest request, TimeSpan ttl)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = request.IsHttps,
            IsEssential = true,
            Path = "/",
            MaxAge = ttl,
        };
    }
}

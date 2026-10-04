using Techodist.Basket.Infrastructure.Caching;
using Microsoft.Extensions.Options;

namespace Techodist.Basket.Api.Baskets;

/// <summary>
/// Достаёт <c>basketId</c> из cookie либо создаёт новый и сразу выдаёт его клиенту.
/// Единственное место, где рождается идентификатор корзины, поэтому cookie и корзина в Redis
/// не могут разойтись.
/// </summary>
public sealed class BasketIdProvider(
    IHttpContextAccessor httpContextAccessor,
    IOptions<BasketStorageOptions> storageOptions)
{
    public Guid GetOrCreateBasketId()
    {
        var context = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("BasketIdProvider доступен только в рамках HTTP-запроса.");

        if (context.Request.Cookies.TryGetValue(BasketCookie.Name, out var raw) &&
            Guid.TryParse(raw, out var fromCookie) &&
            fromCookie != Guid.Empty)
        {
            return fromCookie;
        }

        // Мусор в cookie (или её отсутствие) — не ошибка: гость получает новую корзину.
        var basketId = Guid.NewGuid();

        context.Response.Cookies.Append(
            BasketCookie.Name,
            basketId.ToString(),
            BasketCookie.CreateOptions(context.Request, storageOptions.Value.Ttl));

        return basketId;
    }
}

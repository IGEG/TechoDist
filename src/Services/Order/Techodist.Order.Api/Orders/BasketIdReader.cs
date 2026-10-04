using Microsoft.AspNetCore.Http;

namespace Techodist.Order.Api.Orders;

/// <summary>
/// Чтение <c>basketId</c> из анонимного cookie гостя (ADR 0005). В отличие от Basket API,
/// Order не создаёт корзину: если cookie нет или он битый, оформлять нечего — API вернёт
/// ошибку валидации «order.basket.missing».
/// </summary>
public sealed class BasketIdReader(IHttpContextAccessor httpContextAccessor)
{
    /// <summary>Имя cookie корзины — совпадает с <c>BasketCookie.Name</c> в Basket API.</summary>
    public const string CookieName = "techodist_basket";

    public Guid? Read()
    {
        var context = httpContextAccessor.HttpContext;

        if (context is null ||
            !context.Request.Cookies.TryGetValue(CookieName, out var raw) ||
            !Guid.TryParse(raw, out var basketId) ||
            basketId == Guid.Empty)
        {
            return null;
        }

        return basketId;
    }
}

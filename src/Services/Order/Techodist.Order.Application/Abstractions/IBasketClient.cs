using Techodist.Order.Application.Dtos;

namespace Techodist.Order.Application.Abstractions;

/// <summary>
/// Чтение корзины гостя из Basket (синхронный HTTP-вызов, ADR 0005). Корзина — источник
/// позиций заявки: снимок цен и названий уже лежит в ней, повторно ходить в Catalog не нужно.
/// </summary>
public interface IBasketClient
{
    /// <summary>Текущая корзина либо <c>null</c>, если корзины нет (пустая заявка не оформляется).</summary>
    Task<BasketSnapshot?> GetBasketAsync(Guid basketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Очистка корзины после успешного оформления. Вызывается best-effort: заявка уже сохранена,
    /// и неудача очистки не должна ломать ответ клиенту.
    /// </summary>
    Task ClearBasketAsync(Guid basketId, CancellationToken cancellationToken = default);
}

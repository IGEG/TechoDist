using Techodist.Basket.Application.Dtos;
using Techodist.Basket.Domain.Entities;
using Techodist.Basket.Domain.ValueObjects;
using Mapster;

namespace Techodist.Basket.Application.Common;

/// <summary>
/// Проекция корзины в DTO. Пустая корзина отдаётся и тогда, когда её ещё нет в Redis:
/// для витрины «нет cookie» и «корзина пуста» — один и тот же ответ.
/// </summary>
internal static class BasketDtoMapper
{
    public static BasketDto ToDto(this ShoppingBasket basket) => basket.Adapt<BasketDto>();

    public static BasketDto Empty(Guid basketId)
        => new(basketId, [], 0, 0m, Money.DefaultCurrency);
}

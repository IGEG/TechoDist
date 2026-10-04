using Techodist.Basket.Domain.Entities;
using Techodist.Basket.Domain.ValueObjects;

namespace Techodist.Basket.Infrastructure.Caching;

/// <summary>Перевод агрегата корзины в модель хранения Redis и обратно.</summary>
internal static class BasketStateMapper
{
    public static BasketState FromDomain(ShoppingBasket basket)
        => new(
            basket.Id,
            basket.Items
                .OrderBy(item => item.AddedAt)
                .Select(FromDomain)
                .ToList(),
            basket.CreatedAt,
            basket.UpdatedAt);

    public static ShoppingBasket ToDomain(BasketState state)
        => ShoppingBasket.Rehydrate(
            state.BasketId,
            state.Items.Select(ToDomain),
            state.CreatedAt,
            state.UpdatedAt);

    private static BasketItemState FromDomain(BasketItem item)
        => new(
            item.Id,
            item.ProductId,
            item.ProductName,
            item.ImageUrl,
            item.UnitPrice.Amount,
            item.UnitPrice.Currency,
            item.Quantity,
            item.AddedAt);

    private static BasketItem ToDomain(BasketItemState state)
        => BasketItem.Rehydrate(
            state.Id,
            state.ProductId,
            state.ProductName,
            state.ImageUrl,
            Money.Create(state.UnitPrice, state.Currency),
            state.Quantity,
            state.AddedAt);
}

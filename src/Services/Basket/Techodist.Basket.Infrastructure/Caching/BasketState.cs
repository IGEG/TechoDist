namespace Techodist.Basket.Infrastructure.Caching;

/// <summary>Модель хранения корзины в Redis (ключ <c>basket:{basketId}</c>).</summary>
internal sealed record BasketState(
    Guid BasketId,
    IReadOnlyList<BasketItemState> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

namespace Techodist.Basket.Infrastructure.Caching;

/// <summary>
/// Модель хранения позиции корзины в Redis. Отдельная от домена, чтобы сериализация
/// не диктовала форму агрегата (у домена приватные сеттеры и инварианты).
/// </summary>
internal sealed record BasketItemState(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string? ImageUrl,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    DateTimeOffset AddedAt);

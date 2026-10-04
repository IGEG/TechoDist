using Techodist.Basket.Domain.ValueObjects;
using Techodist.BuildingBlocks.Core.Entities;
using Techodist.BuildingBlocks.Core.Results;

namespace Techodist.Basket.Domain.Entities;

/// <summary>
/// Гостевая корзина — корень агрегата. Идентификатор совпадает с анонимным <c>basketId</c>
/// из HttpOnly-cookie (ADR 0005), поэтому корзину не нужно искать по другим признакам.
/// </summary>
/// <remarks>
/// Имя <c>ShoppingBasket</c>, а не <c>Basket</c>: последний сегмент пространства имён сервиса —
/// <c>Techodist.Basket</c>, и тип с таким же именем компилятор трактует как пространство имён.
/// </remarks>
public sealed class ShoppingBasket : Entity<Guid>, IAggregateRoot
{
    /// <summary>Максимальное число различных товаров в корзине.</summary>
    public const int MaxDistinctItems = 50;

    private readonly List<BasketItem> _items = [];

    private ShoppingBasket()
    {
    }

    private ShoppingBasket(Guid basketId, DateTimeOffset createdAt)
        : base(basketId)
    {
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<BasketItem> Items => _items.AsReadOnly();

    public bool IsEmpty => _items.Count == 0;

    /// <summary>Суммарное количество единиц товара (для бейджа на иконке корзины).</summary>
    public int TotalQuantity => _items.Sum(item => item.Quantity);

    public decimal TotalAmount => _items.Sum(item => item.LineTotal);

    /// <summary>Валюта корзины — валюта первой позиции: магазин работает в одной валюте.</summary>
    public string Currency => _items.Count == 0 ? Money.DefaultCurrency : _items[0].UnitPrice.Currency;

    /// <summary>
    /// Создание корзины. Идентификатор приходит из cookie; если cookie нет —
    /// сервис генерирует новый (и выдаёт его клиенту в ответе).
    /// </summary>
    public static ShoppingBasket Create(Guid? basketId = null)
        => new(basketId is null || basketId == Guid.Empty ? Guid.NewGuid() : basketId.Value, DateTimeOffset.UtcNow);

    /// <summary>Восстановление корзины из хранилища (Redis).</summary>
    public static ShoppingBasket Rehydrate(
        Guid basketId,
        IEnumerable<BasketItem> items,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var basket = new ShoppingBasket(basketId == Guid.Empty ? Guid.NewGuid() : basketId, createdAt)
        {
            UpdatedAt = updatedAt,
        };

        basket._items.AddRange(items.Take(MaxDistinctItems));

        return basket;
    }

    public BasketItem? FindItem(Guid productId) => _items.FirstOrDefault(item => item.ProductId == productId);

    /// <summary>
    /// Добавление товара: новый товар — новая позиция, известный — приращение количества
    /// (с обновлением снимка цены, чтобы покупатель видел актуальную стоимость).
    /// </summary>
    public Result AddItem(
        Guid productId,
        string productName,
        string? imageUrl,
        Money unitPrice,
        int quantity = 1)
    {
        if (productId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("basket.item.product_required", "Товар обязателен."));
        }

        if (quantity < 1 || quantity > BasketItem.MaxQuantity)
        {
            return Result.Failure(Error.Validation(
                "basket.item.quantity_out_of_range",
                $"Количество должно быть от 1 до {BasketItem.MaxQuantity}."));
        }

        var existing = FindItem(productId);

        if (existing is not null)
        {
            existing.RefreshSnapshot(productName, imageUrl, unitPrice);
            existing.AddQuantity(quantity);
            Touch();

            return Result.Success();
        }

        if (_items.Count >= MaxDistinctItems)
        {
            return Result.Failure(Error.Conflict(
                "basket.items.limit_reached",
                $"В корзине не может быть больше {MaxDistinctItems} разных товаров."));
        }

        _items.Add(BasketItem.Create(productId, productName, imageUrl, unitPrice, quantity));
        Touch();

        return Result.Success();
    }

    /// <summary>Изменение количества позиции (покупатель правит количество в корзине).</summary>
    public Result ChangeItemQuantity(Guid productId, int quantity)
    {
        var item = FindItem(productId);

        if (item is null)
        {
            return Result.Failure(ItemNotFound(productId));
        }

        var result = item.ChangeQuantity(quantity);

        if (result.IsSuccess)
        {
            Touch();
        }

        return result;
    }

    /// <summary>Удаление позиции из корзины.</summary>
    public Result RemoveItem(Guid productId)
    {
        var item = FindItem(productId);

        if (item is null)
        {
            return Result.Failure(ItemNotFound(productId));
        }

        _items.Remove(item);
        Touch();

        return Result.Success();
    }

    /// <summary>Полная очистка корзины (идемпотентна).</summary>
    public void Clear()
    {
        if (_items.Count == 0)
        {
            return;
        }

        _items.Clear();
        Touch();
    }

    private static Error ItemNotFound(Guid productId)
        => Error.NotFound("basket.item.not_found", $"Товар '{productId}' отсутствует в корзине.");

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}

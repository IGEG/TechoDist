using Techodist.Basket.Domain.ValueObjects;
using Techodist.BuildingBlocks.Core.Entities;
using Techodist.BuildingBlocks.Core.Results;

namespace Techodist.Basket.Domain.Entities;

/// <summary>
/// Позиция корзины: снимок товара на момент добавления (название, картинка, цена) и количество.
/// Снимок нужен, чтобы корзина читалась без обращения к Catalog (ADR 0005).
/// </summary>
public sealed class BasketItem : Entity<Guid>
{
    /// <summary>Максимальное количество единиц одного товара в корзине.</summary>
    public const int MaxQuantity = 99;

    private BasketItem()
    {
    }

    private BasketItem(
        Guid id,
        Guid productId,
        string productName,
        string? imageUrl,
        Money unitPrice,
        int quantity,
        DateTimeOffset addedAt)
        : base(id)
    {
        ProductId = productId;
        ProductName = productName;
        ImageUrl = imageUrl;
        UnitPrice = unitPrice;
        Quantity = quantity;
        AddedAt = addedAt;
    }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = default!;

    public string? ImageUrl { get; private set; }

    public Money UnitPrice { get; private set; } = default!;

    public int Quantity { get; private set; }

    public DateTimeOffset AddedAt { get; private set; }

    /// <summary>Стоимость позиции (цена × количество).</summary>
    public decimal LineTotal => UnitPrice.Amount * Quantity;

    public static BasketItem Create(
        Guid productId,
        string productName,
        string? imageUrl,
        Money unitPrice,
        int quantity)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Товар обязателен.", nameof(productId));
        }

        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Название товара обязательно.", nameof(productName));
        }

        ArgumentNullException.ThrowIfNull(unitPrice);

        if (quantity < 1 || quantity > MaxQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                $"Количество должно быть от 1 до {MaxQuantity}.");
        }

        return new BasketItem(
            Guid.NewGuid(),
            productId,
            productName.Trim(),
            imageUrl?.Trim(),
            unitPrice,
            quantity,
            DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Восстановление позиции из хранилища (Redis). Количество из хранилища нормализуется,
    /// чтобы повреждённое состояние не выбивало корзину из строя.
    /// </summary>
    public static BasketItem Rehydrate(
        Guid id,
        Guid productId,
        string productName,
        string? imageUrl,
        Money unitPrice,
        int quantity,
        DateTimeOffset addedAt)
        => new(
            id == Guid.Empty ? Guid.NewGuid() : id,
            productId,
            string.IsNullOrWhiteSpace(productName) ? "Товар" : productName.Trim(),
            imageUrl,
            unitPrice,
            Math.Clamp(quantity, 1, MaxQuantity),
            addedAt);

    /// <summary>Установка точного количества (пользователь правит количество в корзине).</summary>
    public Result ChangeQuantity(int quantity)
    {
        if (quantity < 1 || quantity > MaxQuantity)
        {
            return Result.Failure(Error.Validation(
                "basket.item.quantity_out_of_range",
                $"Количество должно быть от 1 до {MaxQuantity}."));
        }

        Quantity = quantity;
        return Result.Success();
    }

    /// <summary>
    /// Увеличение количества при повторном добавлении товара. Выход за лимит обрезается
    /// значением <see cref="MaxQuantity"/> — так покупатель получает максимум, а не ошибку.
    /// </summary>
    public int AddQuantity(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Прибавка количества должна быть положительной.");
        }

        Quantity = Math.Min(MaxQuantity, Quantity + quantity);
        return Quantity;
    }

    /// <summary>Обновление снимка товара при повторном добавлении (актуальная цена и название).</summary>
    public void RefreshSnapshot(string productName, string? imageUrl, Money unitPrice)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Название товара обязательно.", nameof(productName));
        }

        ArgumentNullException.ThrowIfNull(unitPrice);

        ProductName = productName.Trim();
        ImageUrl = imageUrl?.Trim();
        UnitPrice = unitPrice;
    }
}

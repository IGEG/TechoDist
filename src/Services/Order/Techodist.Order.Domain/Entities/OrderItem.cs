using Techodist.BuildingBlocks.Core.Entities;
using Techodist.Order.Domain.ValueObjects;

namespace Techodist.Order.Domain.Entities;

/// <summary>
/// Позиция заявки — снимок товара на момент оформления (название, картинка, цена, количество).
/// Снимок обязателен: заявка должна читаться и через год, когда товар в каталоге уже изменился
/// или снят с продажи.
/// </summary>
public sealed class OrderItem : Entity<Guid>
{
    /// <summary>Максимальное количество единиц одного товара в заявке.</summary>
    public const int MaxQuantity = 99;

    private OrderItem()
    {
    }

    private OrderItem(
        Guid id,
        Guid productId,
        string productName,
        string? imageUrl,
        Money unitPrice,
        int quantity)
        : base(id)
    {
        ProductId = productId;
        ProductName = productName;
        ImageUrl = imageUrl;
        UnitPrice = unitPrice;
        Quantity = quantity;
    }

    public Guid ProductId { get; private set; }

    public string ProductName { get; private set; } = default!;

    public string? ImageUrl { get; private set; }

    public Money UnitPrice { get; private set; } = default!;

    public int Quantity { get; private set; }

    /// <summary>Стоимость позиции (цена × количество).</summary>
    public decimal LineTotal => UnitPrice.Amount * Quantity;

    public static OrderItem Create(
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

        return new OrderItem(
            Guid.NewGuid(),
            productId,
            productName.Trim(),
            imageUrl?.Trim(),
            unitPrice,
            quantity);
    }
}

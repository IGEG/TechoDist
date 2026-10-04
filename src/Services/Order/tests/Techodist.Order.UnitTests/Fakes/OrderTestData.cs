using Techodist.Order.Application.Dtos;
using Techodist.Order.Domain.Entities;
using Techodist.Order.Domain.Enums;
using Techodist.Order.Domain.ValueObjects;

namespace Techodist.Order.UnitTests.Fakes;

/// <summary>Общие данные тестов заявки: цена установки TD60 и позиция «по умолчанию».</summary>
internal static class OrderTestData
{
    public const decimal DefaultPrice = 485_000m;
    public const string DefaultProductName = "Установка Techodist TD60";
    public const string DefaultImageUrl = "/images/products/td-60.png";
    public const string DefaultCustomerName = "Иван Петров";
    public const string DefaultCustomerEmail = "ivan@example.com";

    public static readonly DateOnly DefaultDate = new(2026, 4, 10);

    /// <summary>Читаемый номер заявки в формате <c>TD-ГГГГММДД-00001</c>.</summary>
    public static OrderNumber Number(DateOnly? date = null, long sequence = 1)
        => OrderNumber.Create(date ?? DefaultDate, sequence);

    public static OrderItem Item(
        Guid? productId = null,
        string productName = DefaultProductName,
        decimal price = DefaultPrice,
        int quantity = 1,
        string? imageUrl = DefaultImageUrl)
        => OrderItem.Create(
            productId ?? Guid.NewGuid(),
            productName,
            imageUrl,
            Money.Rub(price),
            quantity);

    public static OrderAggregate Order(
        OrderNumber? number = null,
        Guid? basketId = null,
        IEnumerable<OrderItem>? items = null,
        string customerName = DefaultCustomerName,
        string customerEmail = DefaultCustomerEmail,
        string? customerPhone = null,
        string? comment = null,
        OrderContactChannel preferredChannel = OrderContactChannel.Email,
        OrderPriority priority = OrderPriority.Standard)
        => OrderAggregate.Create(
            number ?? Number(),
            customerName,
            customerEmail,
            items ?? [Item()],
            customerPhone,
            comment,
            preferredChannel,
            priority,
            basketId);

    /// <summary>Снимок корзины гостя так, как его вернул бы Basket API.</summary>
    public static BasketSnapshot Snapshot(
        Guid basketId,
        decimal price = DefaultPrice,
        int quantity = 1,
        string productName = DefaultProductName)
        => new(
            basketId,
            [new BasketSnapshotItem(Guid.NewGuid(), productName, DefaultImageUrl, price, "RUB", quantity)],
            quantity,
            price * quantity,
            "RUB");
}

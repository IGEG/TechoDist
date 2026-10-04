namespace Techodist.Order.Infrastructure.Clients;

/// <summary>
/// Контракт чтения корзины по HTTP (Basket API). Локальная копия формы ответа: сервисы не делят
/// типы, контракт — это JSON, а не общая сборка (ADR 0001).
/// </summary>
internal sealed record BasketResponse(
    Guid BasketId,
    IReadOnlyList<BasketResponseItem> Items,
    int TotalQuantity,
    decimal TotalAmount,
    string Currency);

internal sealed record BasketResponseItem(
    Guid ProductId,
    string ProductName,
    string? ImageUrl,
    decimal UnitPrice,
    string Currency,
    int Quantity);

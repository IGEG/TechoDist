namespace Techodist.Order.Application.Dtos;

/// <summary>
/// Снимок корзины, полученный из Basket. Отдельная модель (не DTO Basket-сервиса): сервисы
/// общаются контрактами по HTTP и не делят типы (ADR 0001).
/// </summary>
public sealed record BasketSnapshot(
    Guid BasketId,
    IReadOnlyList<BasketSnapshotItem> Items,
    int TotalQuantity,
    decimal TotalAmount,
    string Currency)
{
    public bool IsEmpty => Items.Count == 0;
}

/// <summary>Позиция корзины так, как её понимает Order.</summary>
public sealed record BasketSnapshotItem(
    Guid ProductId,
    string ProductName,
    string? ImageUrl,
    decimal UnitPrice,
    string Currency,
    int Quantity);

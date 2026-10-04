namespace Techodist.Order.Application.Dtos;

/// <summary>
/// Заявка целиком: карточка для админки и ответ гостю после оформления.
/// Статусы и каналы отдаются строками — фронтенд и события говорят на одном «языке».
/// </summary>
public sealed record OrderDto(
    Guid Id,
    string Number,
    string Status,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string? Comment,
    string? ManagerComment,
    string PreferredChannel,
    string Priority,
    Guid? BasketId,
    IReadOnlyList<OrderItemDto> Items,
    int TotalQuantity,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

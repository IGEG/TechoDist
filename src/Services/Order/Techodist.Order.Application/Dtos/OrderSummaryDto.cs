namespace Techodist.Order.Application.Dtos;

/// <summary>Краткая строка списка заявок в админке (без позиций — их читают в карточке).</summary>
public sealed record OrderSummaryDto(
    Guid Id,
    string Number,
    string Status,
    string CustomerName,
    string CustomerEmail,
    string Priority,
    int TotalQuantity,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

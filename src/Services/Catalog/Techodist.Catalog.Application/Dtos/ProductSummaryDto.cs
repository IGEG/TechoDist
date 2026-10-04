namespace Techodist.Catalog.Application.Dtos;

/// <summary>Краткая карточка товара (для списков каталога).</summary>
public sealed record ProductSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string? ShortDescription,
    decimal Price,
    string Currency,
    Guid CategoryId,
    string? SolventType,
    int? VolumeLiters,
    string? MainImageUrl,
    string Status);

namespace Techodist.Search.Application.Models;

/// <summary>
/// Снимок опубликованного товара, прочитанный из Catalog API (для реконсиляции индекса).
/// Поля совпадают с <c>ProductSummaryDto</c> каталога.
/// </summary>
public sealed record CatalogProductSnapshot(
    Guid Id,
    string Name,
    string Slug,
    string? ShortDescription,
    decimal Price,
    string Currency,
    Guid CategoryId,
    string? SolventType,
    int? VolumeLiters,
    string? MainImageUrl);

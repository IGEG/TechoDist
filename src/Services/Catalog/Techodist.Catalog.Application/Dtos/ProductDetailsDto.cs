namespace Techodist.Catalog.Application.Dtos;

/// <summary>Детальная карточка товара.</summary>
public sealed record ProductDetailsDto(
    Guid Id,
    string Name,
    string Slug,
    string? ShortDescription,
    string? Description,
    decimal Price,
    string Currency,
    Guid CategoryId,
    string? SolventType,
    int? VolumeLiters,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<ProductImageDto> Images,
    IReadOnlyList<ProductSpecificationDto> Specifications);

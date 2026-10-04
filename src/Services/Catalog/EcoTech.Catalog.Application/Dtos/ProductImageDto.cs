namespace EcoTech.Catalog.Application.Dtos;

public sealed record ProductImageDto(
    Guid Id,
    string Url,
    string? AltText,
    bool IsMain);

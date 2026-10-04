namespace Techodist.Catalog.Application.Dtos;

public sealed record ProductSpecificationDto(
    Guid Id,
    string Name,
    string Value);

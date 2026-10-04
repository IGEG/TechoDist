namespace EcoTech.Catalog.Application.Dtos;

public sealed record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    Guid? ParentId,
    int SortOrder,
    bool IsPublished);

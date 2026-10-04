namespace Techodist.Catalog.Api.Contracts;

/// <summary>Тело запроса на изменение товара (админ-панель); идентификатор берётся из адреса.</summary>
public sealed record UpdateProductRequest(
    string Name,
    Guid CategoryId,
    decimal Price,
    string? ShortDescription = null,
    string? Description = null,
    string? SolventType = null,
    int? VolumeLiters = null,
    string? Slug = null);

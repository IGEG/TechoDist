namespace Techodist.Search.Application.Dtos;

/// <summary>
/// Карточка товара в выдаче поиска. Витрине достаточно этих полей, чтобы отрисовать
/// плитку каталога без обращения к Catalog (данные денормализованы в индексе).
/// </summary>
public sealed record ProductSearchHitDto(
    Guid Id,
    string Name,
    string Slug,
    string? ShortDescription,
    decimal Price,
    string Currency,
    Guid CategoryId,
    string CategoryName,
    string? SolventType,
    int? VolumeLiters,
    string? MainImageUrl);

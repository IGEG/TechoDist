namespace Techodist.Search.Application.Index;

/// <summary>
/// Документ поискового индекса — проекция опубликованного товара каталога (ADR 0009).
/// Это read-модель: у неё нет поведения домена, потому что бизнес-правила живут в Catalog,
/// а Search лишь хранит денормализованный снимок карточки для быстрого поиска.
/// </summary>
public sealed record ProductDocument
{
    /// <summary>Идентификатор товара в каталоге — он же идентификатор документа (<c>_id</c>).</summary>
    public Guid Id { get; init; }

    public string Name { get; init; } = default!;

    public string Slug { get; init; } = default!;

    public string? ShortDescription { get; init; }

    /// <summary>Марка растворителя (например, «Универсальный»).</summary>
    public string? SolventType { get; init; }

    /// <summary>Объём установки, литров.</summary>
    public int? VolumeLiters { get; init; }

    public decimal Price { get; init; }

    public string Currency { get; init; } = "RUB";

    public string? MainImageUrl { get; init; }

    public Guid CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    /// <summary>Признак публикации: индекс хранит только опубликованные товары.</summary>
    public bool IsPublished { get; init; }

    /// <summary>Момент последнего изменения карточки (для диагностики и поиска устаревших документов).</summary>
    public DateTimeOffset UpdatedAt { get; init; }
}

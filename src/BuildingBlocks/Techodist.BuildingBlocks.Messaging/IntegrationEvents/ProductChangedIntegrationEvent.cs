namespace Techodist.BuildingBlocks.Messaging.IntegrationEvents;

public enum ProductChangeType
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
}

/// <summary>
/// Интеграционное событие: товар каталога изменился (для синхронизации поиска).
/// Событие несёт готовый снимок карточки, поэтому Search не обращается к Catalog за данными —
/// обмен только через брокер, как в потоке заявок Order → Notification (ADR 0002, 0009).
/// </summary>
public sealed record ProductChangedIntegrationEvent
{
    public Guid ProductId { get; init; }

    public string Name { get; init; } = default!;

    public string Slug { get; init; } = default!;

    public string? ShortDescription { get; init; }

    public decimal Price { get; init; }

    public string Currency { get; init; } = "RUB";

    /// <summary>Марка растворителя (например, «Универсальный»).</summary>
    public string? SolventType { get; init; }

    /// <summary>Объём установки, литров.</summary>
    public int? VolumeLiters { get; init; }

    public string? MainImageUrl { get; init; }

    public Guid CategoryId { get; init; }

    public string CategoryName { get; init; } = default!;

    /// <summary>
    /// Товар опубликован (виден на витрине). Черновики и архивные карточки в поисковый
    /// индекс не попадают, поэтому потребитель по этому признаку удаляет документ (ADR 0009).
    /// </summary>
    public bool IsPublished { get; init; }

    public ProductChangeType ChangeType { get; init; }

    public DateTimeOffset OccurredAt { get; init; }
}

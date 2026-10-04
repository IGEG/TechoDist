namespace EcoTech.BuildingBlocks.Messaging.IntegrationEvents;

public enum ProductChangeType
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
}

/// <summary>
/// Интеграционное событие: товар каталога изменился (для синхронизации поиска).
/// </summary>
public sealed record ProductChangedIntegrationEvent
{
    public Guid ProductId { get; init; }

    public string Name { get; init; } = default!;

    public string Slug { get; init; } = default!;

    public string? ShortDescription { get; init; }

    public decimal Price { get; init; }

    public Guid CategoryId { get; init; }

    public string CategoryName { get; init; } = default!;

    public bool IsPublished { get; init; }

    public ProductChangeType ChangeType { get; init; }

    public DateTimeOffset OccurredAt { get; init; }
}

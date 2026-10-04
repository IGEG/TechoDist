namespace EcoTech.BuildingBlocks.Core.Events;

/// <summary>
/// Доменное событие — значимый факт, произошедший внутри агрегата.
/// </summary>
public interface IDomainEvent
{
    Guid EventId => Guid.NewGuid();

    DateTimeOffset OccurredOn => DateTimeOffset.UtcNow;
}

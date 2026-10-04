using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Catalog.Domain.Entities;
using Techodist.Catalog.Domain.Enums;

namespace Techodist.Catalog.Application.Common;

/// <summary>
/// Проекция товара в интеграционное событие <see cref="ProductChangedIntegrationEvent"/> (ADR 0009).
/// Событие несёт снимок карточки (цена, категория, изображение), поэтому Search строит документ
/// индекса без обращения к Catalog — сервисы обмениваются только через брокер (ADR 0002).
/// </summary>
internal static class ProductEventsMapper
{
    public static ProductChangedIntegrationEvent ToIntegrationEvent(
        this Product product,
        string? categoryName,
        ProductChangeType changeType) => new()
        {
            ProductId = product.Id,
            Name = product.Name,
            Slug = product.Slug.Value,
            ShortDescription = product.ShortDescription,
            Price = product.Price.Amount,
            Currency = product.Price.Currency,
            SolventType = product.SolventType,
            VolumeLiters = product.VolumeLiters,
            MainImageUrl = MainImageUrl(product),
            CategoryId = product.CategoryId,
            // Для Deleted-события потребителю нужен только идентификатор, поэтому имя категории
            // подставляется по возможности: удаление не должно зависеть от чтения категории.
            CategoryName = categoryName ?? string.Empty,
            IsPublished = product.Status == ProductStatus.Published,
            ChangeType = changeType,
            OccurredAt = product.UpdatedAt ?? product.CreatedAt,
        };

    /// <summary>Главное изображение карточки — то же правило, что и в <c>ProductSummaryDto</c>.</summary>
    private static string? MainImageUrl(Product product) => product.Images
        .OrderByDescending(image => image.IsMain)
        .ThenBy(image => image.SortOrder)
        .Select(image => image.Url)
        .FirstOrDefault();
}

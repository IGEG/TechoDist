using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Search.Application.Index;
using Techodist.Search.Application.Models;

namespace Techodist.Search.Application.Common;

/// <summary>
/// Сборка документа индекса из двух источников: снимка события <c>ProductChanged</c> (обычный путь,
/// ADR 0009) и снимка Catalog API (реконсиляция после сбоев). Оба пути дают одинаковый документ,
/// поэтому индекс не зависит от того, каким способом пришли данные.
/// </summary>
internal static class ProductDocumentMapper
{
    public static ProductDocument FromEvent(ProductChangedIntegrationEvent changed)
    {
        ArgumentNullException.ThrowIfNull(changed);

        return new ProductDocument
        {
            Id = changed.ProductId,
            Name = changed.Name,
            Slug = changed.Slug,
            ShortDescription = changed.ShortDescription,
            SolventType = changed.SolventType,
            VolumeLiters = changed.VolumeLiters,
            Price = changed.Price,
            Currency = changed.Currency,
            MainImageUrl = changed.MainImageUrl,
            CategoryId = changed.CategoryId,
            CategoryName = changed.CategoryName,
            IsPublished = changed.IsPublished,
            UpdatedAt = changed.OccurredAt,
        };
    }

    public static ProductDocument FromSnapshot(
        CatalogProductSnapshot snapshot,
        string? categoryName,
        DateTimeOffset updatedAt)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new ProductDocument
        {
            Id = snapshot.Id,
            Name = snapshot.Name,
            Slug = snapshot.Slug,
            ShortDescription = snapshot.ShortDescription,
            SolventType = snapshot.SolventType,
            VolumeLiters = snapshot.VolumeLiters,
            Price = snapshot.Price,
            Currency = snapshot.Currency,
            MainImageUrl = snapshot.MainImageUrl,
            CategoryId = snapshot.CategoryId,
            // Публичный список каталога отдаёт только опубликованные товары, поэтому признак
            // публикации известен заранее, а имя категории может отсутствовать в снимке.
            CategoryName = categoryName ?? string.Empty,
            IsPublished = true,
            UpdatedAt = updatedAt,
        };
    }
}

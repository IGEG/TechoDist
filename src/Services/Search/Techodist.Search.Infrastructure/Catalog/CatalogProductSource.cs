using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Techodist.Search.Application.Abstractions;
using Techodist.Search.Application.Models;

namespace Techodist.Search.Infrastructure.Catalog;

/// <summary>
/// Чтение каталога через публичные эндпоинты Catalog API (<c>GET api/products</c>,
/// <c>GET api/categories</c>). Нужно только для реконсиляции: обычный путь обновления
/// индекса — событие <c>ProductChanged</c> из брокера (ADR 0009), а API каталога — источник
/// истины, когда событие потерялось.
/// </summary>
internal sealed class CatalogProductSource(HttpClient http, ILogger<CatalogProductSource> logger) : ICatalogProductSource
{
    /// <summary>Размер страницы чтения: совпадает с максимумом, который отдаёт каталог.</summary>
    internal const int PageSize = 100;

    /// <summary>Предохранитель от зацикливания, если каталог начнёт отдавать страницы неверно.</summary>
    internal const int MaxPages = 100;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<CatalogProductSnapshot>> ListPublishedProductsAsync(
        CancellationToken cancellationToken = default)
    {
        var products = new List<CatalogProductSnapshot>();

        for (var page = 1; page <= MaxPages; page++)
        {
            var response = await http.GetFromJsonAsync<CatalogProductPage>(
                $"api/products?page={page}&pageSize={PageSize}",
                JsonOptions,
                cancellationToken)
                ?? throw new InvalidOperationException("Catalog API вернул пустой ответ на запрос списка товаров.");

            products.AddRange(response.Items.Select(item => new CatalogProductSnapshot(
                item.Id,
                item.Name,
                item.Slug,
                item.ShortDescription,
                item.Price,
                item.Currency,
                item.CategoryId,
                item.SolventType,
                item.VolumeLiters,
                item.MainImageUrl)));

            if (page >= response.TotalPages || response.Items.Count == 0)
            {
                break;
            }
        }

        logger.LogInformation("Прочитано {ProductCount} опубликованных товаров из Catalog API.", products.Count);

        return products;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetCategoryNamesAsync(
        CancellationToken cancellationToken = default)
    {
        var categories = await http.GetFromJsonAsync<List<CatalogCategory>>(
            "api/categories",
            JsonOptions,
            cancellationToken) ?? [];

        return categories
            .GroupBy(category => category.Id)
            .ToDictionary(group => group.Key, group => group.First().Name);
    }

    /// <summary>Страница <c>PagedResult&lt;ProductSummaryDto&gt;</c> каталога (нужные поля).</summary>
    internal sealed record CatalogProductPage(IReadOnlyList<CatalogProduct> Items, int TotalPages);

    /// <summary>Поля <c>ProductSummaryDto</c>, которые нужны документу индекса.</summary>
    internal sealed record CatalogProduct(
        Guid Id,
        string Name,
        string Slug,
        string? ShortDescription,
        decimal Price,
        string Currency,
        Guid CategoryId,
        string? SolventType,
        int? VolumeLiters,
        string? MainImageUrl);

    /// <summary>Поля <c>CategoryDto</c>, которые нужны для денормализации имени категории.</summary>
    internal sealed record CatalogCategory(Guid Id, string Name);
}

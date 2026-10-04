using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Techodist.Basket.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Techodist.Basket.Infrastructure.Clients;

/// <summary>
/// Синхронный REST-клиент Catalog: берёт снимок товара для позиции корзины (ADR 0005).
/// Вызывается только при добавлении товара, поэтому цена запроса мала.
/// </summary>
internal sealed class CatalogProductClient(HttpClient httpClient, ILogger<CatalogProductClient> logger)
    : ICatalogProductClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<CatalogProduct?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/products/{productId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Catalog ответил {StatusCode} на запрос товара {ProductId}.",
                (int)response.StatusCode,
                productId);

            throw new HttpRequestException($"Catalog API вернул {(int)response.StatusCode}.");
        }

        var product = await response.Content.ReadFromJsonAsync<CatalogProductResponse>(
            SerializerOptions,
            cancellationToken);

        return product is null
            ? null
            : new CatalogProduct(
                product.Id,
                product.Name,
                ResolveImageUrl(product),
                product.Price,
                product.Currency,
                IsAvailable(product.Status));
    }

    /// <summary>Главное изображение товара (как в списке каталога: основное, затем порядок сортировки).</summary>
    private static string? ResolveImageUrl(CatalogProductResponse product) => product.Images?
        .OrderByDescending(image => image.IsMain)
        .Select(image => image.Url)
        .FirstOrDefault();

    /// <summary>В корзину попадают только опубликованные товары.</summary>
    private static bool IsAvailable(string? status)
        => string.Equals(status, "Published", StringComparison.OrdinalIgnoreCase);

    private sealed record CatalogProductResponse(
        Guid Id,
        string Name,
        decimal Price,
        string Currency,
        string? Status,
        IReadOnlyList<CatalogProductImageResponse>? Images);

    private sealed record CatalogProductImageResponse(Guid Id, string Url, bool IsMain);
}

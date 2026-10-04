using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Techodist.Order.Application.Abstractions;
using Techodist.Order.Application.Dtos;

namespace Techodist.Order.Infrastructure.Clients;

/// <summary>
/// Клиент Basket API. Корзина гостя адресуется тем же анонимным cookie, что и на витрине
/// (ADR 0005), поэтому service-to-service вызов подставляет cookie вручную: у Order нет
/// пользовательского HTTP-контекста.
/// </summary>
internal sealed class BasketClient(HttpClient http, ILogger<BasketClient> logger) : IBasketClient
{
    /// <summary>Имя cookie корзины — совпадает с BasketCookie.Name в Basket API.</summary>
    internal const string BasketCookieName = "techodist_basket";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<BasketSnapshot?> GetBasketAsync(Guid basketId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, basketId);
        using var response = await http.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            logger.LogInformation("Корзина {BasketId} не найдена в Basket API.", basketId);
            return null;
        }

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<BasketResponse>(JsonOptions, cancellationToken);

        if (payload is null || payload.Items.Count == 0)
        {
            return null;
        }

        return new BasketSnapshot(
            payload.BasketId,
            payload.Items
                .Select(item => new BasketSnapshotItem(
                    item.ProductId,
                    item.ProductName,
                    item.ImageUrl,
                    item.UnitPrice,
                    item.Currency,
                    item.Quantity))
                .ToList(),
            payload.TotalQuantity,
            payload.TotalAmount,
            payload.Currency);
    }

    public async Task ClearBasketAsync(Guid basketId, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Delete, basketId);
        using var response = await http.SendAsync(request, cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, Guid basketId)
    {
        var request = new HttpRequestMessage(method, "api/basket");
        request.Headers.Add("Cookie", $"{BasketCookieName}={basketId}");

        return request;
    }
}

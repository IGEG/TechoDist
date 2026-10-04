using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Techodist.Order.Infrastructure.Clients;
using Xunit;

namespace Techodist.Order.UnitTests.Infrastructure;

/// <summary>
/// Клиент Basket API: служба адресует корзину гостя тем же анонимным cookie, что и витрина
/// (ADR 0005), поэтому cookie в запросе — часть контракта.
/// </summary>
public sealed class BasketClientTests
{
    private static readonly Guid BasketId = Guid.Parse("6f0f3d5e-2a3b-4c1d-9e8f-1a2b3c4d5e6f");

    private static readonly Guid ProductId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public async Task GetBasketAsync_SendsGuestCookieAndMapsSnapshot()
    {
        var handler = StubHttpMessageHandler.Ok(BasketJson());
        var client = CreateClient(handler);

        var snapshot = await client.GetBasketAsync(BasketId);

        var request = Assert.Single(handler.Requests);

        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("http://basket.local/api/basket", request.Uri);
        Assert.Equal("techodist_basket=" + BasketId, request.Cookie);

        Assert.NotNull(snapshot);
        Assert.Equal(BasketId, snapshot.BasketId);
        Assert.Equal(2, snapshot.TotalQuantity);
        Assert.Equal(970_000m, snapshot.TotalAmount);
        Assert.Equal("RUB", snapshot.Currency);

        var item = Assert.Single(snapshot.Items);

        Assert.Equal(ProductId, item.ProductId);
        Assert.Equal("Установка Techodist TD60", item.ProductName);
        Assert.Equal("/images/products/td-60.png", item.ImageUrl);
        Assert.Equal(485_000m, item.UnitPrice);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public async Task GetBasketAsync_WhenBasketDoesNotExist_ReturnsNull()
    {
        // Гость без cookie или с «протухшей» корзиной: оформлять нечего, а не 500.
        var handler = new StubHttpMessageHandler(HttpStatusCode.NotFound);
        var client = CreateClient(handler);

        Assert.Null(await client.GetBasketAsync(BasketId));
    }

    [Fact]
    public async Task GetBasketAsync_WhenBasketHasNoItems_ReturnsNull()
    {
        var handler = StubHttpMessageHandler.Ok(BasketJson(withItem: false, totalQuantity: 0, totalAmount: 0));
        var client = CreateClient(handler);

        Assert.Null(await client.GetBasketAsync(BasketId));
    }

    [Fact]
    public async Task GetBasketAsync_WhenBasketServiceFails_Throws()
    {
        // Инфраструктурный сбой не прячем: обработчик отличит его от «корзина пуста».
        var handler = new StubHttpMessageHandler(HttpStatusCode.InternalServerError);
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetBasketAsync(BasketId));
    }

    [Fact]
    public async Task ClearBasketAsync_SendsDeleteWithGuestCookie()
    {
        var handler = StubHttpMessageHandler.Ok("{}");
        var client = CreateClient(handler);

        await client.ClearBasketAsync(BasketId);

        var request = Assert.Single(handler.Requests);

        Assert.Equal(HttpMethod.Delete, request.Method);
        Assert.Equal("http://basket.local/api/basket", request.Uri);
        Assert.Equal("techodist_basket=" + BasketId, request.Cookie);
    }

    private static BasketClient CreateClient(StubHttpMessageHandler handler)
        => new(
            new HttpClient(handler) { BaseAddress = new Uri("http://basket.local") },
            NullLogger<BasketClient>.Instance);

    private static string BasketJson(
        bool withItem = true,
        int totalQuantity = 2,
        decimal totalAmount = 970_000m)
        => JsonSerializer.Serialize(new
        {
            basketId = BasketId,
            items = withItem
                ? new[]
                {
                    new
                    {
                        productId = ProductId,
                        productName = "Установка Techodist TD60",
                        imageUrl = "/images/products/td-60.png",
                        unitPrice = 485_000m,
                        currency = "RUB",
                        quantity = 2,
                    },
                }
                : [],
            totalQuantity,
            totalAmount,
            currency = "RUB",
        });

    /// <summary>Подменный транспорт: запоминает запрос и отдаёт заранее заданный ответ.</summary>
    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string? json = null) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Uri, string? Cookie)> Requests { get; } = [];

        public static StubHttpMessageHandler Ok(string json) => new(HttpStatusCode.OK, json);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add((
                request.Method,
                request.RequestUri!.ToString(),
                request.Headers.TryGetValues("Cookie", out var values) ? string.Join("; ", values) : null));

            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json ?? string.Empty, Encoding.UTF8, "application/json"),
            });
        }
    }
}

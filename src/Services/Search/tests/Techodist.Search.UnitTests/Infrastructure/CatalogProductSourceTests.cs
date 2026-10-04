using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Techodist.Search.Infrastructure.Catalog;
using Xunit;

namespace Techodist.Search.UnitTests.Infrastructure;

/// <summary>
/// Чтение каталога для реконсиляции: публичный список обходится постранично, поля
/// <c>ProductSummaryDto</c> переносятся в снимок, категории читаются отдельным запросом.
/// </summary>
public sealed class CatalogProductSourceTests
{
    private const string FirstPage = """
        {
          "items": [
            {
              "id": "11111111-2222-3333-4444-555555555555",
              "name": "Установка TD60",
              "slug": "ustanovka-td60",
              "shortDescription": "Регенерация 60 л растворителя за смену.",
              "price": 289000,
              "currency": "RUB",
              "categoryId": "99999999-8888-7777-6666-555555555555",
              "solventType": "Универсальный",
              "volumeLiters": 60,
              "mainImageUrl": "/images/products/td60.png",
              "status": "Published"
            }
          ],
          "page": 1,
          "pageSize": 100,
          "totalCount": 2,
          "totalPages": 2
        }
        """;

    private const string SecondPage = """
        {
          "items": [
            {
              "id": "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
              "name": "Установка TD120",
              "slug": "ustanovka-td120",
              "price": 415000,
              "currency": "RUB",
              "categoryId": "99999999-8888-7777-6666-555555555555",
              "status": "Published"
            }
          ],
          "page": 2,
          "pageSize": 100,
          "totalCount": 2,
          "totalPages": 2
        }
        """;

    /// <summary>Первая страница каталога, у которого объявлено больше страниц, чем реально есть данных.</summary>
    private const string FirstPageWithMorePages = """
        {
          "items": [
            {
              "id": "11111111-2222-3333-4444-555555555555",
              "name": "Установка TD60",
              "slug": "ustanovka-td60",
              "price": 289000,
              "currency": "RUB",
              "categoryId": "99999999-8888-7777-6666-555555555555",
              "status": "Published"
            }
          ],
          "page": 1,
          "pageSize": 100,
          "totalCount": 1,
          "totalPages": 5
        }
        """;

    [Fact]
    public async Task ListPublishedProductsAsync_WalksAllPagesAndMapsFields()
    {
        var handler = new StubHttpMessageHandler(request =>
            request.RequestUri!.Query.Contains("page=1", StringComparison.Ordinal)
                ? StubHttpMessageHandler.Json(FirstPage)
                : StubHttpMessageHandler.Json(SecondPage));

        var products = await Source(handler).ListPublishedProductsAsync(CancellationToken.None);

        Assert.Equal(2, products.Count);

        var first = products[0];

        Assert.Equal(Guid.Parse("11111111-2222-3333-4444-555555555555"), first.Id);
        Assert.Equal("Установка TD60", first.Name);
        Assert.Equal("ustanovka-td60", first.Slug);
        Assert.Equal("Регенерация 60 л растворителя за смену.", first.ShortDescription);
        Assert.Equal(289000m, first.Price);
        Assert.Equal("RUB", first.Currency);
        Assert.Equal(Guid.Parse("99999999-8888-7777-6666-555555555555"), first.CategoryId);
        Assert.Equal("Универсальный", first.SolventType);
        Assert.Equal(60, first.VolumeLiters);
        Assert.Equal("/images/products/td60.png", first.MainImageUrl);

        // Вторая страница читается тем же размером страницы — иначе часть товаров потерялась бы.
        Assert.Equal(
            ["/api/products?page=1&pageSize=100", "/api/products?page=2&pageSize=100"],
            handler.Requests);
    }

    [Fact]
    public async Task ListPublishedProductsAsync_EmptyPage_StopsReading()
    {
        const string emptyPage = """
            { "items": [], "page": 2, "pageSize": 100, "totalCount": 1, "totalPages": 5 }
            """;

        var handler = new StubHttpMessageHandler(request =>
            request.RequestUri!.Query.Contains("page=1", StringComparison.Ordinal)
                ? StubHttpMessageHandler.Json(FirstPageWithMorePages)
                : StubHttpMessageHandler.Json(emptyPage));

        var products = await Source(handler).ListPublishedProductsAsync(CancellationToken.None);

        // Каталог отдал пустую страницу — дальше идти незачем, даже если totalPages больше.
        Assert.Single(products);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task GetCategoryNamesAsync_MapsIdToName()
    {
        const string categories = """
            [
              {
                "id": "99999999-8888-7777-6666-555555555555",
                "name": "Установки регенерации",
                "slug": "ustanovki-regeneracii",
                "description": null,
                "parentId": null,
                "sortOrder": 1,
                "isPublished": true
              }
            ]
            """;

        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(categories));

        var names = await Source(handler).GetCategoryNamesAsync(CancellationToken.None);

        Assert.Equal(
            "Установки регенерации",
            names[Guid.Parse("99999999-8888-7777-6666-555555555555")]);
        Assert.Equal(["/api/categories"], handler.Requests);
    }

    private static CatalogProductSource Source(StubHttpMessageHandler handler)
        => new(new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5101") },
            NullLogger<CatalogProductSource>.Instance);
}

/// <summary>Заглушка HTTP: отвечает подготовленными страницами и запоминает запрошенные адреса.</summary>
internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    private readonly List<string> _requests = [];

    public IReadOnlyList<string> Requests => _requests;

    public static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _requests.Add(request.RequestUri!.PathAndQuery);

        return Task.FromResult(responder(request));
    }
}

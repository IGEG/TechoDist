using Mapster;
using Techodist.Search.Application.Common;
using Techodist.Search.Application.Features.SearchProducts;
using Techodist.Search.Application.Index;
using Techodist.Search.Application.Models;
using Techodist.Search.UnitTests.Fakes;
using Xunit;

namespace Techodist.Search.UnitTests.Application;

/// <summary>
/// Запрос витрины «поиск товаров»: нормализация параметров, отображение документов индекса
/// в DTO выдачи и поведение на заведомо пустой выдаче без обращения к Elasticsearch.
/// </summary>
public sealed class SearchProductsQueryHandlerTests
{
    private readonly FakeProductIndex _index = new();

    public SearchProductsQueryHandlerTests()
    {
        // Тот же маппинг, что регистрирует AddSearchApplication.
        SearchMappingConfig.Register(TypeAdapterConfig.GlobalSettings);
    }

    [Fact]
    public async Task Handle_NormalizesQueryAndMapsHits()
    {
        _index.SearchResult = new ProductIndexPage([Document()], TotalCount: 120);

        var result = await Handler().Handle(
            new SearchProductsQuery(Q: "  TD60  ", Sort: "price_desc", Page: 0, PageSize: 500),
            CancellationToken.None);

        var filter = Assert.IsType<ProductSearchFilter>(_index.LastFilter);

        Assert.Equal("TD60", filter.NormalizedQuery);
        Assert.Equal(ProductSort.PriceDesc, filter.Sort);

        // Страница и размер приводятся к допустимым значениям до обращения к индексу.
        Assert.Equal(1, result.Page);
        Assert.Equal(ProductSearchFilter.MaxPageSize, result.PageSize);
        Assert.Equal(120, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.True(result.HasNext);

        var hit = Assert.Single(result.Items);

        Assert.Equal(SearchTestData.ProductId, hit.Id);
        Assert.Equal(SearchTestData.ProductName, hit.Name);
        Assert.Equal(SearchTestData.Price, hit.Price);
        Assert.Equal("RUB", hit.Currency);
        Assert.Equal(SearchTestData.CategoryName, hit.CategoryName);
        Assert.Equal(SearchTestData.VolumeLiters, hit.VolumeLiters);
        Assert.Equal(SearchTestData.MainImageUrl, hit.MainImageUrl);
    }

    [Fact]
    public async Task Handle_EmptyQuery_FilterHasNoText()
    {
        await Handler().Handle(new SearchProductsQuery(Q: "   "), CancellationToken.None);

        Assert.Null(_index.LastFilter!.NormalizedQuery);
    }

    [Fact]
    public async Task Handle_UnknownSortKey_FallsBackToRelevance()
    {
        await Handler().Handle(new SearchProductsQuery(Sort: "cheapest"), CancellationToken.None);

        Assert.Equal(ProductSort.Relevance, _index.LastFilter!.Sort);
    }

    [Fact]
    public async Task Handle_DefaultPageSize_UsesStorefrontPageSize()
    {
        var result = await Handler().Handle(new SearchProductsQuery(), CancellationToken.None);

        Assert.Equal(ProductSearchFilter.DefaultPageSize, result.PageSize);
    }

    [Fact]
    public async Task Handle_MinPriceAboveMaxPrice_ReturnsEmptyWithoutHittingIndex()
    {
        var result = await Handler().Handle(
            new SearchProductsQuery(MinPrice: 500_000m, MaxPrice: 100_000m),
            CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);

        // Пустая выдача известна заранее — обращение к Elasticsearch только тратило бы время.
        Assert.Null(_index.LastFilter);
    }

    private static ProductDocument Document() => new()
    {
        Id = SearchTestData.ProductId,
        Name = SearchTestData.ProductName,
        Slug = SearchTestData.ProductSlug,
        ShortDescription = SearchTestData.ProductDescription,
        SolventType = SearchTestData.SolventType,
        VolumeLiters = SearchTestData.VolumeLiters,
        Price = SearchTestData.Price,
        Currency = "RUB",
        MainImageUrl = SearchTestData.MainImageUrl,
        CategoryId = SearchTestData.CategoryId,
        CategoryName = SearchTestData.CategoryName,
        IsPublished = true,
        UpdatedAt = SearchTestData.OccurredAt,
    };

    private SearchProductsQueryHandler Handler() => new(_index);
}

using EcoTech.Catalog.Application.Common;
using EcoTech.Catalog.Application.Features.Products.Queries.GetProducts;
using EcoTech.Catalog.Domain.Entities;
using EcoTech.Catalog.Domain.ValueObjects;
using EcoTech.Catalog.UnitTests.Fakes;
using Mapster;
using Xunit;

namespace EcoTech.Catalog.UnitTests.Application;

public sealed class GetProductsQueryHandlerTests
{
    public GetProductsQueryHandlerTests()
    {
        CatalogMappingConfig.Register(TypeAdapterConfig.GlobalSettings);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult_AndCaches()
    {
        var categoryId = Guid.NewGuid();
        var repository = new FakeProductRepository();

        for (var index = 0; index < 3; index++)
        {
            var product = Product.Create(
                $"Установка {index}",
                categoryId,
                Money.Rub(1_000m + index),
                solventType: "Универсальный");
            product.Publish();
            repository.Seed(product);
        }

        var cache = new FakeCacheService();
        var handler = new GetProductsQueryHandler(repository, cache);

        var first = await handler.Handle(new GetProductsQuery(Page: 1, PageSize: 2), CancellationToken.None);
        var second = await handler.Handle(new GetProductsQuery(Page: 1, PageSize: 2), CancellationToken.None);

        Assert.Equal(3, first.TotalCount);
        Assert.Equal(2, first.Items.Count);
        Assert.True(first.HasNext);

        // Второй вызов берётся из кэша — значение не перезаписывается.
        Assert.Equal(1, cache.SetCalls);
        Assert.Equal(first.TotalCount, second.TotalCount);
        Assert.Equal(first.Items[0].Id, second.Items[0].Id);
    }
}

using EcoTech.BuildingBlocks.Core.Results;
using EcoTech.Catalog.Application.Common;
using EcoTech.Catalog.Application.Features.Products.Queries.GetProductById;
using EcoTech.Catalog.Domain.Entities;
using EcoTech.Catalog.Domain.ValueObjects;
using EcoTech.Catalog.UnitTests.Fakes;
using Mapster;
using Xunit;

namespace EcoTech.Catalog.UnitTests.Application;

public sealed class GetProductByIdQueryHandlerTests
{
    public GetProductByIdQueryHandlerTests()
    {
        CatalogMappingConfig.Register(TypeAdapterConfig.GlobalSettings);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNotFoundError()
    {
        var handler = new GetProductByIdQueryHandler(new FakeProductRepository());

        var result = await handler.Handle(new GetProductByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_ExistingProduct_ReturnsMappedDetails()
    {
        var product = Product.Create(
            "Установка регенерации растворителей Techodist TD60",
            Guid.NewGuid(),
            Money.Rub(485_000m),
            "Кратко",
            "Подробно",
            "Универсальный",
            60);
        product.Publish();
        product.AddImage("/images/techodist-td60.jpg", "Techodist TD60", isMain: true);

        var repository = new FakeProductRepository();
        repository.Seed(product);

        var handler = new GetProductByIdQueryHandler(repository);

        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("ustanovka-regeneratsii-rastvoriteley-techodist-td60", result.Value.Slug);
        Assert.Equal(485_000m, result.Value.Price);
        Assert.Equal("RUB", result.Value.Currency);
        Assert.Equal("Published", result.Value.Status);
        Assert.Single(result.Value.Images);
    }
}

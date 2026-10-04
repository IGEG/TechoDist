using Techodist.Basket.Application.Common;
using Techodist.Basket.Application.Features.Baskets.Commands.AddBasketItem;
using Techodist.Basket.UnitTests.Fakes;
using Techodist.BuildingBlocks.Core.Results;
using Mapster;
using Xunit;

namespace Techodist.Basket.UnitTests.Application;

public sealed class AddBasketItemCommandHandlerTests
{
    public AddBasketItemCommandHandlerTests()
    {
        BasketMappingConfig.Register(TypeAdapterConfig.GlobalSettings);
    }

    [Fact]
    public async Task Handle_UnknownProduct_ReturnsNotFound()
    {
        var repository = new FakeBasketRepository();
        var handler = new AddBasketItemCommandHandler(repository, new FakeCatalogProductClient());

        var result = await handler.Handle(
            new AddBasketItemCommand(Guid.NewGuid(), Guid.NewGuid(), 1),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("basket.product.not_found", result.Error.Code);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Handle_UnavailableProduct_ReturnsValidationFailure()
    {
        var basketId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var repository = new FakeBasketRepository();
        var catalog = new FakeCatalogProductClient();
        catalog.Add(BasketTestData.Product(productId, isAvailable: false));

        var result = await new AddBasketItemCommandHandler(repository, catalog)
            .Handle(new AddBasketItemCommand(basketId, productId, 1), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("basket.product.unavailable", result.Error.Code);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Handle_NewBasket_CreatesBasketWithCookieId()
    {
        var basketId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var repository = new FakeBasketRepository();
        var catalog = new FakeCatalogProductClient();
        catalog.Add(BasketTestData.Product(productId));

        var result = await new AddBasketItemCommandHandler(repository, catalog)
            .Handle(new AddBasketItemCommand(basketId, productId, 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.Contains(basketId));
        Assert.Equal(1, repository.SaveCalls);

        var dto = result.Value;
        Assert.Equal(basketId, dto.BasketId);
        Assert.Equal(970_000m, dto.TotalAmount);
        Assert.Equal(2, dto.TotalQuantity);

        var item = Assert.Single(dto.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(BasketTestData.DefaultProductName, item.ProductName);
        Assert.Equal(BasketTestData.DefaultImageUrl, item.ImageUrl);
        Assert.Equal(BasketTestData.DefaultPrice, item.UnitPrice);
    }

    [Fact]
    public async Task Handle_ExistingItem_IncreasesQuantity()
    {
        var basketId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var repository = new FakeBasketRepository();
        repository.Seed(BasketTestData.Basket(basketId, productId, quantity: 1));
        var catalog = new FakeCatalogProductClient();
        catalog.Add(BasketTestData.Product(productId));

        var result = await new AddBasketItemCommandHandler(repository, catalog)
            .Handle(new AddBasketItemCommand(basketId, productId, 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.SaveCalls);
        Assert.Equal(3, Assert.Single(result.Value.Items).Quantity);
    }

    [Fact]
    public async Task Handle_ExistingBasket_AddsSecondPosition()
    {
        var basketId = Guid.NewGuid();
        var existingProductId = Guid.NewGuid();
        var newProductId = Guid.NewGuid();
        var repository = new FakeBasketRepository();
        repository.Seed(BasketTestData.Basket(basketId, existingProductId));
        var catalog = new FakeCatalogProductClient();
        catalog.Add(BasketTestData.Product(newProductId, name: "Растворитель", price: 1_500m, imageUrl: null));

        var result = await new AddBasketItemCommandHandler(repository, catalog)
            .Handle(new AddBasketItemCommand(basketId, newProductId, 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(BasketTestData.DefaultPrice + 1_500m, result.Value.TotalAmount);
        Assert.Equal(1, catalog.Calls);
    }
}

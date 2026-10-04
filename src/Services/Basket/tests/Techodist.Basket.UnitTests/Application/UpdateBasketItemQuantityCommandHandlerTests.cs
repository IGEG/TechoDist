using Techodist.Basket.Application.Common;
using Techodist.Basket.Application.Features.Baskets.Commands.UpdateBasketItemQuantity;
using Techodist.Basket.UnitTests.Fakes;
using Techodist.BuildingBlocks.Core.Results;
using Mapster;
using Xunit;

namespace Techodist.Basket.UnitTests.Application;

public sealed class UpdateBasketItemQuantityCommandHandlerTests
{
    public UpdateBasketItemQuantityCommandHandlerTests()
    {
        BasketMappingConfig.Register(TypeAdapterConfig.GlobalSettings);
    }

    [Fact]
    public async Task Handle_MissingBasket_ReturnsNotFound()
    {
        var repository = new FakeBasketRepository();

        var result = await new UpdateBasketItemQuantityCommandHandler(repository)
            .Handle(new UpdateBasketItemQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), 2), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("basket.not_found", result.Error.Code);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Handle_UnknownItem_ReturnsNotFound()
    {
        var basketId = Guid.NewGuid();
        var repository = new FakeBasketRepository();
        repository.Seed(BasketTestData.Basket(basketId, Guid.NewGuid()));

        var result = await new UpdateBasketItemQuantityCommandHandler(repository)
            .Handle(new UpdateBasketItemQuantityCommand(basketId, Guid.NewGuid(), 2), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("basket.item.not_found", result.Error.Code);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task Handle_ValidQuantity_UpdatesAndSaves()
    {
        var basketId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var repository = new FakeBasketRepository();
        repository.Seed(BasketTestData.Basket(basketId, productId, quantity: 1));

        var result = await new UpdateBasketItemQuantityCommandHandler(repository)
            .Handle(new UpdateBasketItemQuantityCommand(basketId, productId, 5), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, repository.SaveCalls);
        Assert.Equal(5, Assert.Single(result.Value.Items).Quantity);
        Assert.Equal(BasketTestData.DefaultPrice * 5, result.Value.TotalAmount);
    }

    [Fact]
    public async Task Handle_OutOfRangeQuantity_ReturnsValidationFailure()
    {
        var basketId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var repository = new FakeBasketRepository();
        repository.Seed(BasketTestData.Basket(basketId, productId, quantity: 3));

        var result = await new UpdateBasketItemQuantityCommandHandler(repository)
            .Handle(new UpdateBasketItemQuantityCommand(basketId, productId, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, repository.SaveCalls);

        var stored = await repository.GetAsync(basketId, CancellationToken.None);
        Assert.Equal(3, stored!.FindItem(productId)!.Quantity);
    }
}

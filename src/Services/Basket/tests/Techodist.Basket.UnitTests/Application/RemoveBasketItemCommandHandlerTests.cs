using Techodist.Basket.Application.Common;
using Techodist.Basket.Application.Features.Baskets.Commands.RemoveBasketItem;
using Techodist.Basket.Domain.ValueObjects;
using Techodist.Basket.UnitTests.Fakes;
using Techodist.BuildingBlocks.Core.Results;
using Mapster;
using Xunit;

namespace Techodist.Basket.UnitTests.Application;

public sealed class RemoveBasketItemCommandHandlerTests
{
    public RemoveBasketItemCommandHandlerTests()
    {
        BasketMappingConfig.Register(TypeAdapterConfig.GlobalSettings);
    }

    [Fact]
    public async Task Handle_MissingBasket_ReturnsNotFound()
    {
        var repository = new FakeBasketRepository();

        var result = await new RemoveBasketItemCommandHandler(repository)
            .Handle(new RemoveBasketItemCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("basket.not_found", result.Error.Code);
    }

    [Fact]
    public async Task Handle_UnknownItem_ReturnsNotFound()
    {
        var basketId = Guid.NewGuid();
        var repository = new FakeBasketRepository();
        repository.Seed(BasketTestData.Basket(basketId, Guid.NewGuid()));

        var result = await new RemoveBasketItemCommandHandler(repository)
            .Handle(new RemoveBasketItemCommand(basketId, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("basket.item.not_found", result.Error.Code);
        Assert.Empty(repository.DeletedBasketIds);
    }

    [Fact]
    public async Task Handle_RemainingItems_SavesBasket()
    {
        var basketId = Guid.NewGuid();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var repository = new FakeBasketRepository();
        var basket = BasketTestData.Basket(basketId, first);
        basket.AddItem(second, "Растворитель", null, Money.Rub(1_500m), 1);
        repository.Seed(basket);

        var result = await new RemoveBasketItemCommandHandler(repository)
            .Handle(new RemoveBasketItemCommand(basketId, first), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(repository.DeletedBasketIds);
        Assert.Equal(1, repository.SaveCalls);
        Assert.Equal(second, Assert.Single(result.Value.Items).ProductId);
    }

    [Fact]
    public async Task Handle_LastItem_DeletesBasketAndReturnsEmpty()
    {
        var basketId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var repository = new FakeBasketRepository();
        repository.Seed(BasketTestData.Basket(basketId, productId));

        var result = await new RemoveBasketItemCommandHandler(repository)
            .Handle(new RemoveBasketItemCommand(basketId, productId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, repository.SaveCalls); // корзина уже была в хранилище — handler её удаляет, а не сохраняет
        Assert.Equal([basketId], repository.DeletedBasketIds);
        Assert.False(repository.Contains(basketId));
        Assert.Empty(result.Value.Items);
        Assert.Equal(basketId, result.Value.BasketId);
    }
}

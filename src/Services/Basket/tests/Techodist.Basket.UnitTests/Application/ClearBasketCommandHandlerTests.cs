using Techodist.Basket.Application.Features.Baskets.Commands.ClearBasket;
using Techodist.Basket.UnitTests.Fakes;
using Xunit;

namespace Techodist.Basket.UnitTests.Application;

public sealed class ClearBasketCommandHandlerTests
{
    [Fact]
    public async Task Handle_ExistingBasket_DeletesItAndReturnsEmpty()
    {
        var basketId = Guid.NewGuid();
        var repository = new FakeBasketRepository();
        repository.Seed(BasketTestData.Basket(basketId, Guid.NewGuid()));

        var dto = await new ClearBasketCommandHandler(repository)
            .Handle(new ClearBasketCommand(basketId), CancellationToken.None);

        Assert.Equal([basketId], repository.DeletedBasketIds);
        Assert.False(repository.Contains(basketId));
        Assert.Equal(basketId, dto.BasketId);
        Assert.Empty(dto.Items);
        Assert.Equal(0m, dto.TotalAmount);
    }

    [Fact]
    public async Task Handle_MissingBasket_IsIdempotent()
    {
        var basketId = Guid.NewGuid();
        var repository = new FakeBasketRepository();

        var dto = await new ClearBasketCommandHandler(repository)
            .Handle(new ClearBasketCommand(basketId), CancellationToken.None);

        Assert.Equal([basketId], repository.DeletedBasketIds);
        Assert.Equal(basketId, dto.BasketId);
        Assert.Empty(dto.Items);
    }
}

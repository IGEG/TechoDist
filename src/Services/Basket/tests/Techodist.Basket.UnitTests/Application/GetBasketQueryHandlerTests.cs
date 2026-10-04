using Techodist.Basket.Application.Common;
using Techodist.Basket.Application.Features.Baskets.Queries.GetBasket;
using Techodist.Basket.Domain.Entities;
using Techodist.Basket.Domain.ValueObjects;
using Techodist.Basket.UnitTests.Fakes;
using Mapster;
using Xunit;

namespace Techodist.Basket.UnitTests.Application;

public sealed class GetBasketQueryHandlerTests
{
    public GetBasketQueryHandlerTests()
    {
        BasketMappingConfig.Register(TypeAdapterConfig.GlobalSettings);
    }

    [Fact]
    public async Task Handle_MissingBasket_ReturnsEmptyBasket()
    {
        var basketId = Guid.NewGuid();
        var handler = new GetBasketQueryHandler(new FakeBasketRepository());

        var dto = await handler.Handle(new GetBasketQuery(basketId), CancellationToken.None);

        // Нет cookie / нет корзины — витрина получает пустую корзину с тем же идентификатором.
        Assert.Equal(basketId, dto.BasketId);
        Assert.Empty(dto.Items);
        Assert.Equal(0, dto.TotalQuantity);
        Assert.Equal(0m, dto.TotalAmount);
        Assert.Equal("RUB", dto.Currency);
    }

    [Fact]
    public async Task Handle_ExistingBasket_ReturnsMappedItemsAndTotals()
    {
        var basketId = Guid.NewGuid();
        var installationId = Guid.NewGuid();
        var solventId = Guid.NewGuid();
        var repository = new FakeBasketRepository();
        var basket = ShoppingBasket.Create(basketId);

        basket.AddItem(installationId, "Установка TD-60", "/images/td-60.png", Money.Rub(485_000m), 1);
        basket.AddItem(solventId, "Растворитель", null, Money.Rub(1_500m), 3);
        repository.Seed(basket);

        var dto = await new GetBasketQueryHandler(repository)
            .Handle(new GetBasketQuery(basketId), CancellationToken.None);

        Assert.Equal(basketId, dto.BasketId);
        Assert.Equal(2, dto.Items.Count);
        Assert.Equal(4, dto.TotalQuantity);
        Assert.Equal(489_500m, dto.TotalAmount);

        Assert.Equal(installationId, dto.Items[0].ProductId);
        Assert.Equal(485_000m, dto.Items[0].UnitPrice);
        Assert.Equal("RUB", dto.Items[0].Currency);
        Assert.Equal("/images/td-60.png", dto.Items[0].ImageUrl);

        Assert.Equal(solventId, dto.Items[1].ProductId);
        Assert.Equal(4_500m, dto.Items[1].LineTotal);
        Assert.Null(dto.Items[1].ImageUrl);
    }
}

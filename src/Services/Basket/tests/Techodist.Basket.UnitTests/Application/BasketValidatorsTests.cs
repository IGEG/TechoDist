using Techodist.Basket.Application.Features.Baskets.Commands.AddBasketItem;
using Techodist.Basket.Application.Features.Baskets.Commands.RemoveBasketItem;
using Techodist.Basket.Application.Features.Baskets.Commands.UpdateBasketItemQuantity;
using Techodist.Basket.Domain.Entities;
using Xunit;

namespace Techodist.Basket.UnitTests.Application;

public sealed class BasketValidatorsTests
{
    [Fact]
    public void AddBasketItemCommandValidator_ValidCommand_Passes()
    {
        Assert.True(new AddBasketItemCommandValidator()
            .Validate(new AddBasketItemCommand(Guid.NewGuid(), Guid.NewGuid(), 1)).IsValid);
    }

    [Fact]
    public void AddBasketItemCommandValidator_EmptyIds_Fail()
    {
        var result = new AddBasketItemCommandValidator()
            .Validate(new AddBasketItemCommand(Guid.Empty, Guid.Empty, 1));

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(BasketItem.MaxQuantity + 1)]
    public void AddBasketItemCommandValidator_QuantityOutOfRange_Fails(int quantity)
    {
        var result = new AddBasketItemCommandValidator()
            .Validate(new AddBasketItemCommand(Guid.NewGuid(), Guid.NewGuid(), quantity));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AddBasketItemCommand.Quantity));
    }

    [Fact]
    public void UpdateBasketItemQuantityCommandValidator_QuantityOutOfRange_Fails()
    {
        var result = new UpdateBasketItemQuantityCommandValidator()
            .Validate(new UpdateBasketItemQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), 0));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void UpdateBasketItemQuantityCommandValidator_ValidCommand_Passes()
    {
        Assert.True(new UpdateBasketItemQuantityCommandValidator()
            .Validate(new UpdateBasketItemQuantityCommand(Guid.NewGuid(), Guid.NewGuid(), BasketItem.MaxQuantity)).IsValid);
    }

    [Fact]
    public void RemoveBasketItemCommandValidator_EmptyIds_Fail()
    {
        var result = new RemoveBasketItemCommandValidator()
            .Validate(new RemoveBasketItemCommand(Guid.Empty, Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }
}

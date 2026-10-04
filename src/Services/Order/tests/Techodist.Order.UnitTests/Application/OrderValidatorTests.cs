using Techodist.Order.Application.Features.Orders.Commands.ChangeOrderStatus;
using Techodist.Order.Application.Features.Orders.Commands.SubmitOrder;
using Techodist.Order.Domain.Enums;
using Techodist.Order.UnitTests.Fakes;
using Xunit;

namespace Techodist.Order.UnitTests.Application;

/// <summary>Валидация входа: она отсекает заявки, которые нельзя обработать, ещё до обработчика.</summary>
public sealed class OrderValidatorTests
{
    private static SubmitOrderCommand ValidSubmit
        => new(
            Guid.NewGuid(),
            OrderTestData.DefaultCustomerName,
            OrderTestData.DefaultCustomerEmail,
            CustomerPhone: "+7 (999) 000-00-00",
            Comment: "Нужна доставка в Тверь",
            PreferredChannel: OrderContactChannel.Phone,
            Priority: OrderPriority.Standard);

    [Fact]
    public void SubmitOrder_ValidCommand_PassesValidation()
    {
        var result = new SubmitOrderCommandValidator().Validate(ValidSubmit);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SubmitOrder_WithoutNameOrEmail_IsRejected(string value)
    {
        var validator = new SubmitOrderCommandValidator();

        Assert.False(validator.Validate(ValidSubmit with { CustomerName = value }).IsValid);
        Assert.False(validator.Validate(ValidSubmit with { CustomerEmail = value }).IsValid);
    }

    [Theory]
    [InlineData("ivan.example.com")]
    [InlineData("ivan@")]
    [InlineData("@example.com")]
    public void SubmitOrder_MalformedEmail_IsRejected(string email)
    {
        var result = new SubmitOrderCommandValidator().Validate(ValidSubmit with { CustomerEmail = email });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SubmitOrder_PhoneWithLetters_IsRejected()
    {
        var result = new SubmitOrderCommandValidator().Validate(ValidSubmit with { CustomerPhone = "8-800-TECHO" });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SubmitOrder_WithoutPhone_IsValidBecauseItIsOptional()
    {
        var result = new SubmitOrderCommandValidator().Validate(ValidSubmit with { CustomerPhone = null });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SubmitOrder_WithoutBasket_IsRejected()
    {
        var result = new SubmitOrderCommandValidator().Validate(ValidSubmit with { BasketId = Guid.Empty });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SubmitOrder_OverlongFields_AreRejected()
    {
        var validator = new SubmitOrderCommandValidator();

        Assert.False(validator.Validate(ValidSubmit with { CustomerName = new string('И', 201) }).IsValid);
        Assert.False(validator.Validate(ValidSubmit with { CustomerEmail = $"ivan@{new string('a', 260)}.ru" }).IsValid);
        Assert.False(validator.Validate(ValidSubmit with { Comment = new string('a', 2001) }).IsValid);
    }

    [Fact]
    public void SubmitOrder_UnknownContactChannel_IsRejected()
    {
        var result = new SubmitOrderCommandValidator()
            .Validate(ValidSubmit with { PreferredChannel = (OrderContactChannel)99 });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SubmitOrder_UnknownPriority_IsRejected()
    {
        var result = new SubmitOrderCommandValidator()
            .Validate(ValidSubmit with { Priority = (OrderPriority)77 });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ChangeOrderStatus_ValidCommand_PassesValidation()
    {
        var result = new ChangeOrderStatusCommandValidator()
            .Validate(new ChangeOrderStatusCommand(Guid.NewGuid(), OrderStatus.Confirmed, "Созвонились"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ChangeOrderStatus_WithoutOrderId_IsRejected()
    {
        var result = new ChangeOrderStatusCommandValidator()
            .Validate(new ChangeOrderStatusCommand(Guid.Empty, OrderStatus.Confirmed));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ChangeOrderStatus_UnknownStatus_IsRejected()
    {
        var result = new ChangeOrderStatusCommandValidator()
            .Validate(new ChangeOrderStatusCommand(Guid.NewGuid(), (OrderStatus)99));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ChangeOrderStatus_OverlongManagerComment_IsRejected()
    {
        var result = new ChangeOrderStatusCommandValidator()
            .Validate(new ChangeOrderStatusCommand(Guid.NewGuid(), OrderStatus.Confirmed, new string('a', 2001)));

        Assert.False(result.IsValid);
    }
}

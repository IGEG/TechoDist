using Techodist.BuildingBlocks.Core.Results;
using Techodist.Order.Domain.Enums;
using Techodist.Order.UnitTests.Fakes;
using Xunit;

namespace Techodist.Order.UnitTests.Domain;

/// <summary>
/// Воронка статусов: разрешены только шаги «вперёд» и отмена, повтор статуса — конфликт
/// (иначе клиент получил бы два письма об одном переходе).
/// </summary>
public sealed class OrderStatusTransitionTests
{
    [Fact]
    public void ChangeStatus_AllowsNextStepOfFunnel()
    {
        var order = OrderTestData.Order();

        var result = order.ChangeStatus(OrderStatus.Confirmed, "  Созвонились, всё в силе ");

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal("Созвонились, всё в силе", order.ManagerComment);
    }

    [Fact]
    public void ChangeStatus_RepeatedStatus_IsConflict()
    {
        var order = OrderTestData.Order();

        var result = order.ChangeStatus(OrderStatus.Pending);

        Assert.True(result.IsFailure);
        Assert.Equal("order.status.already_set", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Theory]
    [InlineData(OrderStatus.InProgress)]
    [InlineData(OrderStatus.Completed)]
    public void ChangeStatus_SkippingFunnelStep_IsConflict(OrderStatus target)
    {
        var order = OrderTestData.Order();

        var result = order.ChangeStatus(target);

        Assert.True(result.IsFailure);
        Assert.Equal("order.status.transition_not_allowed", result.Error.Code);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void ChangeStatus_TerminalOrder_IsConflict()
    {
        var order = At(OrderStatus.Completed);

        Assert.True(order.IsFinal);
        Assert.Equal("order.status.transition_not_allowed", order.ChangeStatus(OrderStatus.Cancelled).Error.Code);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.InProgress)]
    public void ChangeStatus_ToCancelled_IsAllowedFromAnyActiveStage(OrderStatus from)
    {
        var order = At(from);

        var result = order.ChangeStatus(OrderStatus.Cancelled, "Клиент передумал");

        Assert.True(result.IsSuccess);
        Assert.True(order.IsFinal);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Pending, OrderStatus.InProgress, false)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.InProgress, true)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Completed, false)]
    [InlineData(OrderStatus.InProgress, OrderStatus.Completed, true)]
    [InlineData(OrderStatus.Completed, OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending, false)]
    public void CanTransitionTo_MatchesFunnel(OrderStatus from, OrderStatus to, bool expected)
        => Assert.Equal(expected, At(from).CanTransitionTo(to));

    [Theory]
    [InlineData(OrderStatus.Pending, false)]
    [InlineData(OrderStatus.Confirmed, false)]
    [InlineData(OrderStatus.InProgress, false)]
    [InlineData(OrderStatus.Completed, true)]
    [InlineData(OrderStatus.Cancelled, true)]
    public void IsFinal_IsTrueOnlyForTerminalStatuses(OrderStatus status, bool expected)
        => Assert.Equal(expected, At(status).IsFinal);

    /// <summary>Заявка, доведённая до нужного статуса по воронке.</summary>
    private static OrderAggregate At(OrderStatus status)
    {
        var order = OrderTestData.Order();

        if (status is OrderStatus.Confirmed or OrderStatus.InProgress or OrderStatus.Completed)
        {
            order.ChangeStatus(OrderStatus.Confirmed);
        }

        if (status is OrderStatus.InProgress or OrderStatus.Completed)
        {
            order.ChangeStatus(OrderStatus.InProgress);
        }

        if (status is OrderStatus.Completed)
        {
            order.ChangeStatus(OrderStatus.Completed);
        }

        if (status is OrderStatus.Cancelled)
        {
            order.ChangeStatus(OrderStatus.Cancelled);
        }

        Assert.Equal(status, order.Status);

        return order;
    }
}

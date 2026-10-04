using Microsoft.Extensions.Logging.Abstractions;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.Order.Application.Features.Orders.Commands.ChangeOrderStatus;
using Techodist.Order.Domain.Enums;
using Techodist.Order.UnitTests.Fakes;
using Xunit;
using IntegrationEvents = Techodist.BuildingBlocks.Messaging.IntegrationEvents;

namespace Techodist.Order.UnitTests.Application;

/// <summary>
/// Смена статуса менеджером: агрегат решает, что переход допустим, а факт изменения уходит
/// клиенту письмом через событие (ADR 0003).
/// </summary>
public sealed class ChangeOrderStatusCommandHandlerTests
{
    private readonly CallLog _log = new();

    private readonly FakeOrderRepository _orders;

    private readonly FakePublishEndpoint _publish;

    public ChangeOrderStatusCommandHandlerTests()
    {
        MappingFixture.EnsureRegistered();

        _orders = new FakeOrderRepository(_log);
        _publish = new FakePublishEndpoint(_log);
    }

    [Fact]
    public async Task Handle_UnknownOrder_ReportsNotFound()
    {
        var result = await Handle(Guid.NewGuid(), OrderStatus.Confirmed);

        Assert.True(result.IsFailure);
        Assert.Equal("order.not_found", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal(0, _orders.SaveCalls);
        Assert.Empty(_publish.Published);
    }

    [Fact]
    public async Task Handle_RepeatedStatus_IsConflictAndStaysSilent()
    {
        // Повтор не должен отправить клиенту второе письмо об одном переходе.
        var order = Seed();

        var result = await Handle(order.Id, OrderStatus.Pending);

        Assert.True(result.IsFailure);
        Assert.Equal("order.status.already_set", result.Error.Code);
        Assert.Equal(0, _orders.SaveCalls);
        Assert.Empty(_publish.Published);
    }

    [Fact]
    public async Task Handle_SkippedStep_IsConflict()
    {
        var order = Seed();

        var result = await Handle(order.Id, OrderStatus.Completed);

        Assert.True(result.IsFailure);
        Assert.Equal("order.status.transition_not_allowed", result.Error.Code);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Empty(_publish.Published);
    }

    [Fact]
    public async Task Handle_ValidTransition_UpdatesStatusAndPublishesStatusEvent()
    {
        var order = Seed();

        var result = await Handle(order.Id, OrderStatus.Confirmed, "Созвонились, всё в силе");

        Assert.True(result.IsSuccess);
        Assert.Equal("Confirmed", result.Value.Status);
        Assert.Equal("Созвонились, всё в силе", result.Value.ManagerComment);

        var published = Assert.Single(_publish.PublishedOf<IntegrationEvents.OrderStatusChangedIntegrationEvent>());

        Assert.Equal(order.Id, published.OrderId);
        Assert.Equal(order.Number.Value, published.OrderNumber);
        Assert.Equal(order.CustomerEmail, published.CustomerEmail);
        Assert.Equal(nameof(OrderStatus.Pending), published.OldStatus);
        Assert.Equal(nameof(OrderStatus.Confirmed), published.NewStatus);
        Assert.Equal("Созвонились, всё в силе", published.ManagerComment);
        Assert.Equal(1, _orders.SaveCalls);
    }

    [Fact]
    public async Task Handle_ValidTransition_PublishesEventBeforeSavingChanges()
    {
        var order = Seed();

        await Handle(order.Id, OrderStatus.Confirmed);

        Assert.Equal(["publish:OrderStatusChangedIntegrationEvent", "save"], _log.Entries);
    }

    [Fact]
    public async Task Handle_TerminalOrder_CannotBeChanged()
    {
        var order = Seed();

        await Handle(order.Id, OrderStatus.Confirmed);
        await Handle(order.Id, OrderStatus.InProgress);
        await Handle(order.Id, OrderStatus.Completed);

        _publish.Reset();

        var result = await Handle(order.Id, OrderStatus.Cancelled);

        Assert.True(result.IsFailure);
        Assert.Equal("order.status.transition_not_allowed", result.Error.Code);
        Assert.Empty(_publish.Published);
    }

    private OrderAggregate Seed()
    {
        var order = OrderTestData.Order(basketId: Guid.NewGuid());
        _orders.Seed(order);

        return order;
    }

    private static ChangeOrderStatusCommandHandler Handler(
        FakeOrderRepository orders,
        FakePublishEndpoint publish)
        => new(orders, publish, NullLogger<ChangeOrderStatusCommandHandler>.Instance);

    private Task<Result<Techodist.Order.Application.Dtos.OrderDto>> Handle(
        Guid orderId,
        OrderStatus status,
        string? managerComment = null)
        => Handler(_orders, _publish)
            .Handle(new ChangeOrderStatusCommand(orderId, status, managerComment), CancellationToken.None);
}

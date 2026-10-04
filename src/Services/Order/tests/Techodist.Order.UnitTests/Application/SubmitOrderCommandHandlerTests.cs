using Microsoft.Extensions.Logging.Abstractions;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.Order.Application.Features.Orders.Commands.SubmitOrder;
using Techodist.Order.Domain.Enums;
using Techodist.Order.UnitTests.Fakes;
using Xunit;
using IntegrationEvents = Techodist.BuildingBlocks.Messaging.IntegrationEvents;

namespace Techodist.Order.UnitTests.Application;

/// <summary>
/// Оформление заявки: позиции берутся из корзины гостя, событие уходит в outbox до сохранения,
/// а корзина очищается best-effort (ADR 0003, ADR 0005).
/// </summary>
public sealed class SubmitOrderCommandHandlerTests
{
    private readonly CallLog _log = new();

    private readonly FakeOrderRepository _orders;

    private readonly FakePublishEndpoint _publish;

    private readonly FakeOrderNumberGenerator _numbers = new();

    public SubmitOrderCommandHandlerTests()
    {
        MappingFixture.EnsureRegistered();

        _orders = new FakeOrderRepository(_log);
        _publish = new FakePublishEndpoint(_log);
    }

    [Fact]
    public async Task Handle_WithoutBasket_CannotBeSubmitted()
    {
        var basket = new FakeBasketClient(OrderTestData.Snapshot(Guid.NewGuid()));

        var result = await Handler(basket).Handle(Command(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("order.basket.missing", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, basket.GetCalls);
        Assert.Equal(0, _orders.SaveCalls);
        Assert.Empty(_publish.Published);
    }

    [Fact]
    public async Task Handle_WhenBasketServiceReturnsNothing_CannotBeSubmitted()
    {
        var result = await Handler(new FakeBasketClient()).Handle(Command(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("order.basket.empty", result.Error.Code);
        Assert.Equal(0, _orders.SaveCalls);
    }

    [Fact]
    public async Task Handle_WhenBasketIsEmpty_CannotBeSubmitted()
    {
        var basketId = Guid.NewGuid();
        var snapshot = OrderTestData.Snapshot(basketId) with
        {
            Items = [],
            TotalQuantity = 0,
            TotalAmount = 0m,
        };

        var result = await Handler(new FakeBasketClient(snapshot)).Handle(Command(basketId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("order.basket.empty", result.Error.Code);
        Assert.Equal(0, _numbers.Calls);
    }

    [Fact]
    public async Task Handle_ValidBasket_CreatesPendingOrderFromSnapshot()
    {
        var basketId = Guid.NewGuid();
        var basket = new FakeBasketClient(OrderTestData.Snapshot(basketId, price: 485_000m, quantity: 2));

        var result = await Handler(basket).Handle(Command(basketId), CancellationToken.None);

        Assert.True(result.IsSuccess);

        var dto = result.Value;

        Assert.Equal("Pending", dto.Status);
        Assert.Equal(OrderTestData.DefaultCustomerEmail, dto.CustomerEmail);
        Assert.Equal(basketId, dto.BasketId);
        Assert.Equal(970_000m, dto.TotalAmount);
        Assert.Equal(2, dto.TotalQuantity);
        Assert.Equal("RUB", dto.Currency);

        var item = Assert.Single(dto.Items);

        Assert.Equal(OrderTestData.DefaultProductName, item.ProductName);
        Assert.Equal(485_000m, item.UnitPrice);
        Assert.Equal(2, item.Quantity);

        Assert.Equal(1, _numbers.Calls);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), _numbers.LastDate);
        Assert.Equal(1, _orders.SaveCalls);
        Assert.Equal(dto.Number, Assert.Single(_orders.AddedOrders).Number.Value);
    }

    [Fact]
    public async Task Handle_ValidBasket_PublishesSubmittedEventWithFullSnapshot()
    {
        var basketId = Guid.NewGuid();
        var basket = new FakeBasketClient(OrderTestData.Snapshot(basketId, price: 485_000m, quantity: 2));

        await Handler(basket).Handle(Command(basketId), CancellationToken.None);

        var order = Assert.Single(_orders.AddedOrders);
        var published = Assert.Single(_publish.PublishedOf<IntegrationEvents.OrderSubmittedIntegrationEvent>());

        Assert.Equal(order.Id, published.OrderId);
        Assert.Equal(order.Number.Value, published.OrderNumber);
        Assert.Equal(order.CustomerName, published.CustomerName);
        Assert.Equal(order.CustomerEmail, published.CustomerEmail);
        Assert.Equal(order.CustomerPhone, published.CustomerPhone);
        Assert.Equal(970_000m, published.TotalAmount);
        Assert.Equal(IntegrationEvents.OrderChannel.Phone, published.PreferredChannel);
        Assert.Equal(IntegrationEvents.OrderPriority.Urgent, published.Priority);
        Assert.Equal(order.CreatedAt, published.SubmittedAt);

        var item = Assert.Single(published.Items);

        Assert.Equal(OrderTestData.DefaultProductName, item.ProductName);
        Assert.Equal(485_000m, item.UnitPrice);
        Assert.Equal(2, item.Quantity);
    }

    [Fact]
    public async Task Handle_ValidBasket_PublishesEventBeforeSavingChanges()
    {
        // Так событие попадает в таблицу outbox той же транзакцией, что и заявка (ADR 0003).
        var basketId = Guid.NewGuid();

        await Handler(new FakeBasketClient(OrderTestData.Snapshot(basketId)))
            .Handle(Command(basketId), CancellationToken.None);

        Assert.Equal(
            ["publish:OrderSubmittedIntegrationEvent", "save"],
            _log.Entries);
    }

    [Fact]
    public async Task Handle_ValidBasket_ClearsGuestBasket()
    {
        var basketId = Guid.NewGuid();
        var basket = new FakeBasketClient(OrderTestData.Snapshot(basketId));

        await Handler(basket).Handle(Command(basketId), CancellationToken.None);

        Assert.Equal(1, basket.ClearCalls);
        Assert.Equal(basketId, basket.ClearedBasketId);
    }

    [Fact]
    public async Task Handle_WhenBasketCannotBeCleared_StillReturnsSuccess()
    {
        // Заявка уже сохранена, событие в outbox: недоступность Basket не отменяет оформление.
        var basketId = Guid.NewGuid();
        var basket = new FakeBasketClient(OrderTestData.Snapshot(basketId))
        {
            ClearFailure = new HttpRequestException("Basket API недоступен."),
        };

        var result = await Handler(basket).Handle(Command(basketId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, _orders.SaveCalls);
        Assert.Single(_publish.PublishedOf<IntegrationEvents.OrderSubmittedIntegrationEvent>());
    }

    private SubmitOrderCommandHandler Handler(FakeBasketClient basket)
        => new(_orders, basket, _numbers, _publish, NullLogger<SubmitOrderCommandHandler>.Instance);

    private static SubmitOrderCommand Command(Guid basketId)
        => new(
            basketId,
            OrderTestData.DefaultCustomerName,
            OrderTestData.DefaultCustomerEmail,
            CustomerPhone: "+7 999 000-00-00",
            Comment: "Просьба позвонить перед доставкой",
            PreferredChannel: OrderContactChannel.Phone,
            Priority: OrderPriority.Urgent);
}

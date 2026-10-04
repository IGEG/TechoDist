using Microsoft.Extensions.Logging.Abstractions;
using Techodist.Order.Application.Features.Orders.Queries.GetOrderById;
using Techodist.Order.Application.Features.Orders.Queries.GetOrderByNumber;
using Techodist.Order.Application.Features.Orders.Queries.GetOrders;
using Techodist.Order.Domain.Enums;
using Techodist.Order.UnitTests.Fakes;
using Xunit;

namespace Techodist.Order.UnitTests.Application;

/// <summary>Чтение заявок: карточка по Id, публичный статус по номеру и список для админки.</summary>
public sealed class OrderQueryHandlerTests
{
    private readonly FakeOrderRepository _orders = new();

    public OrderQueryHandlerTests() => MappingFixture.EnsureRegistered();

    [Fact]
    public async Task GetById_UnknownOrder_ReportsNotFound()
    {
        var handler = new GetOrderByIdQueryHandler(_orders);

        var result = await handler.Handle(new GetOrderByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("order.not_found", result.Error.Code);
    }

    [Fact]
    public async Task GetById_ExistingOrder_ReturnsCardWithItems()
    {
        var order = OrderTestData.Order(items: [OrderTestData.Item(quantity: 2)]);
        _orders.Seed(order);

        var handler = new GetOrderByIdQueryHandler(_orders);

        var result = await handler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(order.Number.Value, result.Value.Number);
        Assert.Equal(nameof(OrderStatus.Pending), result.Value.Status);
        Assert.Single(result.Value.Items);
        Assert.Equal(2, result.Value.TotalQuantity);
    }

    [Fact]
    public async Task GetByNumber_MalformedNumber_IsValidationError()
    {
        var handler = new GetOrderByNumberQueryHandler(_orders);

        var result = await handler.Handle(new GetOrderByNumberQuery("TD-2026-1"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("order.number.invalid", result.Error.Code);
        Assert.Equal(0, _orders.GetByNumberCalls);
    }

    [Fact]
    public async Task GetByNumber_UnknownNumber_ReportsNotFound()
    {
        var handler = new GetOrderByNumberQueryHandler(_orders);

        var result = await handler.Handle(
            new GetOrderByNumberQuery(OrderTestData.Number().Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("order.not_found", result.Error.Code);
    }

    [Fact]
    public async Task GetByNumber_ExistingOrder_IsFoundByLowerCaseNumber()
    {
        // Гость вводит номер руками — регистр и лишние пробелы не должны мешать.
        var order = OrderTestData.Order(number: OrderTestData.Number(sequence: 42));
        _orders.Seed(order);

        var handler = new GetOrderByNumberQueryHandler(_orders);

        var result = await handler.Handle(
            new GetOrderByNumberQuery($"  {order.Number.Value.ToLowerInvariant()}  "),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(order.Number.Value, result.Value.Number);
    }
}

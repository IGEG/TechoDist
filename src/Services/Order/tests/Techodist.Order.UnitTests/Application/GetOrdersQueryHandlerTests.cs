using Microsoft.Extensions.Logging.Abstractions;
using Techodist.Order.Application.Features.Orders.Queries.GetOrders;
using Techodist.Order.Application.Models;
using Techodist.Order.Domain.Enums;
using Techodist.Order.UnitTests.Fakes;
using Xunit;

namespace Techodist.Order.UnitTests.Application;

/// <summary>Список заявок для админки: фильтр по статусу, поиск и пагинация.</summary>
public sealed class GetOrdersQueryHandlerTests
{
    private readonly FakeOrderRepository _orders = new();

    private readonly GetOrdersQueryHandler _handler;

    public GetOrdersQueryHandlerTests()
    {
        MappingFixture.EnsureRegistered();

        _handler = new GetOrdersQueryHandler(_orders);
    }

    [Fact]
    public async Task Handle_WhenNothingMatches_ReturnsEmptyPage()
    {
        var result = await _handler.Handle(new GetOrdersQuery(), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.False(result.HasNext);
        Assert.False(result.HasPrevious);
    }

    [Fact]
    public async Task Handle_FiltersByStatus()
    {
        var pending = OrderTestData.Order();
        var confirmed = OrderTestData.Order();
        confirmed.ChangeStatus(OrderStatus.Confirmed);

        _orders.Seed(pending);
        _orders.Seed(confirmed);

        var result = await _handler.Handle(new GetOrdersQuery(Status: OrderStatus.Confirmed), CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(confirmed.Number.Value, Assert.Single(result.Items).Number);
    }

    [Fact]
    public async Task Handle_SearchesByNumberAndCustomer()
    {
        var order = OrderTestData.Order(number: OrderTestData.Number(sequence: 7), customerName: "Мария Ким");
        _orders.Seed(order);
        _orders.Seed(OrderTestData.Order(number: OrderTestData.Number(sequence: 8)));

        var byNumber = await _handler.Handle(new GetOrdersQuery(Search: order.Number.Value), CancellationToken.None);
        var byName = await _handler.Handle(new GetOrdersQuery(Search: "мария"), CancellationToken.None);

        Assert.Equal(order.Number.Value, Assert.Single(byNumber.Items).Number);
        Assert.Equal(order.Number.Value, Assert.Single(byName.Items).Number);
    }

    [Fact]
    public async Task Handle_PagesResults()
    {
        for (var i = 0; i < 3; i++)
        {
            _orders.Seed(OrderTestData.Order(number: OrderTestData.Number(sequence: i + 1)));
        }

        var secondPage = await _handler.Handle(new GetOrdersQuery(Page: 2, PageSize: 2), CancellationToken.None);

        Assert.Equal(3, secondPage.TotalCount);
        Assert.Equal(2, secondPage.TotalPages);
        Assert.Single(secondPage.Items);
        Assert.Equal(2, secondPage.Page);
        Assert.False(secondPage.HasNext);
        Assert.True(secondPage.HasPrevious);
    }

    [Fact]
    public async Task Handle_NormalizesPagingArguments()
    {
        // Некорректные параметры приводятся к безопасным значениям, а не уходят в SQL как есть.
        for (var i = 0; i < 3; i++)
        {
            _orders.Seed(OrderTestData.Order(number: OrderTestData.Number(sequence: i + 1)));
        }

        var clamped = await _handler.Handle(new GetOrdersQuery(Page: 0, PageSize: 5000), CancellationToken.None);

        Assert.Equal(1, clamped.Page);
        Assert.Equal(OrderListFilter.MaxPageSize, clamped.PageSize);
        Assert.Equal(3, clamped.Items.Count);
    }

    [Fact]
    public async Task Handle_SummaryRows_HaveNoItemsButKeepTotals()
    {
        var order = OrderTestData.Order(items: [OrderTestData.Item(quantity: 3)]);
        _orders.Seed(order);

        var result = await _handler.Handle(new GetOrdersQuery(), CancellationToken.None);

        var summary = Assert.Single(result.Items);

        Assert.Equal(order.Number.Value, summary.Number);
        Assert.Equal(nameof(OrderStatus.Pending), summary.Status);
        Assert.Equal(3, summary.TotalQuantity);
        Assert.Equal(OrderTestData.DefaultPrice * 3, summary.TotalAmount);
        Assert.Equal(order.CreatedAt, summary.CreatedAt);
        Assert.Equal(1, _orders.ListCalls);
    }
}

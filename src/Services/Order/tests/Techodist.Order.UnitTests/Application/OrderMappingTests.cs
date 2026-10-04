using Techodist.Order.Application.Common;
using Techodist.Order.Domain.Enums;
using Techodist.Order.UnitTests.Fakes;
using Xunit;
using IntegrationEvents = Techodist.BuildingBlocks.Messaging.IntegrationEvents;

namespace Techodist.Order.UnitTests.Application;

/// <summary>Проекции заявки: DTO для API и интеграционные события для Notification (ADR 0003).</summary>
public sealed class OrderMappingTests
{
    public OrderMappingTests() => MappingFixture.EnsureRegistered();

    [Fact]
    public void ToDto_ProjectsDomainValuesToStringsAndFlattensMoney()
    {
        var order = OrderTestData.Order(items: [OrderTestData.Item(price: 485_000m, quantity: 2)]);

        var dto = order.ToDto();

        Assert.Equal(order.Id, dto.Id);
        Assert.Equal(order.Number.Value, dto.Number);
        Assert.Equal(nameof(OrderStatus.Pending), dto.Status);
        Assert.Equal(nameof(OrderContactChannel.Email), dto.PreferredChannel);
        Assert.Equal(nameof(OrderPriority.Standard), dto.Priority);
        Assert.Equal(970_000m, dto.TotalAmount);
        Assert.Equal(2, dto.TotalQuantity);
        Assert.Equal("RUB", dto.Currency);

        var item = Assert.Single(dto.Items);

        Assert.Equal(OrderTestData.DefaultProductName, item.ProductName);
        Assert.Equal(OrderTestData.DefaultImageUrl, item.ImageUrl);
        Assert.Equal(485_000m, item.UnitPrice);
        Assert.Equal("RUB", item.Currency);
        Assert.Equal(970_000m, item.LineTotal);
    }

    [Fact]
    public void ToSummaryDto_ProjectsRowForAdminList()
    {
        var order = OrderTestData.Order(items: [OrderTestData.Item(quantity: 4)]);

        var summary = order.ToSummaryDto();

        Assert.Equal(order.Number.Value, summary.Number);
        Assert.Equal(4, summary.TotalQuantity);
        Assert.Equal(OrderTestData.DefaultPrice * 4, summary.TotalAmount);
        Assert.Equal("RUB", summary.Currency);
        Assert.Equal(order.UpdatedAt, summary.UpdatedAt);
    }

    [Fact]
    public void ToSubmittedEvent_MapsContactsTotalsAndItems()
    {
        var order = OrderTestData.Order(
            items: [OrderTestData.Item(quantity: 3)],
            customerPhone: "+7 999 000-00-00",
            preferredChannel: OrderContactChannel.Phone,
            priority: OrderPriority.Urgent);

        var integrationEvent = order.ToSubmittedEvent();

        Assert.Equal(order.Id, integrationEvent.OrderId);
        Assert.Equal("+7 999 000-00-00", integrationEvent.CustomerPhone);
        Assert.Equal(IntegrationEvents.OrderChannel.Phone, integrationEvent.PreferredChannel);
        Assert.Equal(IntegrationEvents.OrderPriority.Urgent, integrationEvent.Priority);
        Assert.Equal(order.TotalAmount, integrationEvent.TotalAmount);

        var item = Assert.Single(integrationEvent.Items);

        Assert.Equal(3, item.Quantity);
        Assert.Equal(OrderTestData.DefaultPrice, item.UnitPrice);
    }

    [Fact]
    public void ToSubmittedEvent_DefaultsToEmailAndStandardPriority()
    {
        var integrationEvent = OrderTestData.Order().ToSubmittedEvent();

        Assert.Equal(IntegrationEvents.OrderChannel.Email, integrationEvent.PreferredChannel);
        Assert.Equal(IntegrationEvents.OrderPriority.Standard, integrationEvent.Priority);
        Assert.Null(integrationEvent.Comment);
    }

    [Fact]
    public void ToStatusChangedEvent_CarriesStatusesAsStringsForEmailTemplate()
    {
        var order = OrderTestData.Order();
        order.ChangeStatus(OrderStatus.Confirmed, "Подтвердили по телефону");

        var integrationEvent = order.ToStatusChangedEvent(OrderStatus.Pending, "Подтвердили по телефону");

        Assert.Equal(order.Id, integrationEvent.OrderId);
        Assert.Equal(order.Number.Value, integrationEvent.OrderNumber);
        Assert.Equal(nameof(OrderStatus.Pending), integrationEvent.OldStatus);
        Assert.Equal(nameof(OrderStatus.Confirmed), integrationEvent.NewStatus);
        Assert.Equal("Подтвердили по телефону", integrationEvent.ManagerComment);
        Assert.Equal(order.UpdatedAt, integrationEvent.ChangedAt);
    }
}

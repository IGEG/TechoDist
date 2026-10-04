using Techodist.Order.Application.Dtos;
using Mapster;

namespace Techodist.Order.Application.Common;

/// <summary>Проекция заявки в DTO (Mapster-правила — в <see cref="OrderMappingConfig"/>).</summary>
internal static class OrderDtoMapper
{
    public static OrderDto ToDto(this OrderAggregate order) => order.Adapt<OrderDto>();

    public static OrderSummaryDto ToSummaryDto(this OrderAggregate order) => order.Adapt<OrderSummaryDto>();
}

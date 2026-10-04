using Techodist.Order.Application.Dtos;
using Techodist.Order.Domain.Entities;
using Mapster;

namespace Techodist.Order.Application.Common;

/// <summary>Правила маппинга заявки в DTO (Mapster).</summary>
public static class OrderMappingConfig
{
    public static void Register(TypeAdapterConfig config)
    {
        config.NewConfig<OrderItem, OrderItemDto>()
            .Map(dest => dest.UnitPrice, src => src.UnitPrice.Amount)
            .Map(dest => dest.Currency, src => src.UnitPrice.Currency);

        config.NewConfig<OrderAggregate, OrderDto>()
            .Map(dest => dest.Number, src => src.Number.Value)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.PreferredChannel, src => src.PreferredChannel.ToString())
            .Map(dest => dest.Priority, src => src.Priority.ToString())
            .Map(dest => dest.Items, src => src.Items.Select(item => item.Adapt<OrderItemDto>()).ToList());

        config.NewConfig<OrderAggregate, OrderSummaryDto>()
            .Map(dest => dest.Number, src => src.Number.Value)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Priority, src => src.Priority.ToString());
    }
}

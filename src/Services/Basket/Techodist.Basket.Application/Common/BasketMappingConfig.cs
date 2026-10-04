using Techodist.Basket.Application.Dtos;
using Techodist.Basket.Domain.Entities;
using Mapster;

namespace Techodist.Basket.Application.Common;

/// <summary>Правила маппинга агрегата корзины в DTO (Mapster).</summary>
public static class BasketMappingConfig
{
    public static void Register(TypeAdapterConfig config)
    {
        config.NewConfig<BasketItem, BasketItemDto>()
            .Map(dest => dest.UnitPrice, src => src.UnitPrice.Amount)
            .Map(dest => dest.Currency, src => src.UnitPrice.Currency);

        config.NewConfig<ShoppingBasket, BasketDto>()
            .Map(dest => dest.BasketId, src => src.Id)
            .Map(dest => dest.Items, src => src.Items
                .OrderBy(item => item.AddedAt)
                .Select(item => item.Adapt<BasketItemDto>())
                .ToList());
    }
}

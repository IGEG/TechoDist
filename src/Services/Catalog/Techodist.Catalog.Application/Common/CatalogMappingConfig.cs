using Techodist.Catalog.Application.Dtos;
using Techodist.Catalog.Domain.Entities;
using Mapster;

namespace Techodist.Catalog.Application.Common;

/// <summary>Правила маппинга доменных сущностей каталога в DTO (Mapster).</summary>
public static class CatalogMappingConfig
{
    public static void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Category, CategoryDto>()
            .Map(dest => dest.Slug, src => src.Slug.Value);

        config.NewConfig<Product, ProductSummaryDto>()
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.Price, src => src.Price.Amount)
            .Map(dest => dest.Currency, src => src.Price.Currency)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.MainImageUrl, src => src.Images
                .OrderByDescending(i => i.IsMain)
                .ThenBy(i => i.SortOrder)
                .Select(i => i.Url)
                .FirstOrDefault());

        config.NewConfig<Product, ProductDetailsDto>()
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.Price, src => src.Price.Amount)
            .Map(dest => dest.Currency, src => src.Price.Currency)
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.Images, src => src.Images.OrderBy(i => i.SortOrder).ToList())
            .Map(dest => dest.Specifications, src => src.Specifications.OrderBy(s => s.SortOrder).ToList());
    }
}

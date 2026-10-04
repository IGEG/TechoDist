using Techodist.Search.Application.Dtos;
using Techodist.Search.Application.Index;
using Mapster;

namespace Techodist.Search.Application.Common;

/// <summary>Правила маппинга документов индекса в DTO выдачи (Mapster).</summary>
public static class SearchMappingConfig
{
    public static void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ProductDocument, ProductSearchHitDto>();
    }
}

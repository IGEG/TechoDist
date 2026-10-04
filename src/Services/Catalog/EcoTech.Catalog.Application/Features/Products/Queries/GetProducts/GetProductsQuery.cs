using EcoTech.Catalog.Application.Abstractions;
using EcoTech.Catalog.Application.Dtos;
using EcoTech.Catalog.Application.Models;
using Mapster;
using MediatR;

namespace EcoTech.Catalog.Application.Features.Products.Queries.GetProducts;

public sealed record GetProductsQuery(
    Guid? CategoryId = null,
    string? SolventType = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 12,
    string? Sort = null) : IRequest<PagedResult<ProductSummaryDto>>;

internal sealed class GetProductsQueryHandler(
    IProductRepository products,
    ICacheService cache)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductSummaryDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    public async Task<PagedResult<ProductSummaryDto>> Handle(
        GetProductsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new ProductListFilter(
            request.CategoryId,
            request.SolventType,
            request.Search,
            OnlyPublished: true,
            request.Page,
            request.PageSize,
            request.Sort);

        var cacheKey = $"catalog:products:{filter.CategoryId}:{filter.SolventType}:{filter.Search}:{filter.NormalizedPage}:{filter.NormalizedPageSize}:{filter.Sort}";

        return await cache.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                var (items, totalCount) = await products.ListAsync(filter, ct);

                var dtos = items
                    .Select(product => product.Adapt<ProductSummaryDto>())
                    .ToList();

                return PagedResult<ProductSummaryDto>.Create(
                    dtos,
                    totalCount,
                    filter.NormalizedPage,
                    filter.NormalizedPageSize);
            },
            CacheTtl,
            cancellationToken);
    }
}

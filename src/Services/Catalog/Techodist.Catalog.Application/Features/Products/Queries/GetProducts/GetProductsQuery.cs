using Techodist.BuildingBlocks.Core.Pagination;
using Techodist.Catalog.Application.Abstractions;
using Techodist.Catalog.Application.Common;
using Techodist.Catalog.Application.Dtos;
using Techodist.Catalog.Application.Models;
using Mapster;
using MediatR;

namespace Techodist.Catalog.Application.Features.Products.Queries.GetProducts;

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

        // Версия каталога в ключе: изменение товара в админке делает старые страницы недостижимыми.
        var version = await cache.GetAsync<long?>(CatalogCacheKeys.ProductListVersion, cancellationToken) ?? 0;
        var cacheKey = CatalogCacheKeys.ProductList(filter, version);

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

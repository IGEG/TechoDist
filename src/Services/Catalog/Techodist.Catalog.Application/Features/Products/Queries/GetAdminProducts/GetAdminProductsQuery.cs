using MediatR;
using Techodist.BuildingBlocks.Core.Pagination;
using Techodist.Catalog.Application.Abstractions;
using Techodist.Catalog.Application.Dtos;
using Techodist.Catalog.Application.Models;
using Techodist.Catalog.Domain.Enums;
using Mapster;

namespace Techodist.Catalog.Application.Features.Products.Queries.GetAdminProducts;

/// <summary>
/// Список товаров для админ-панели: в отличие от витрины показывает и черновики, и архив
/// (менеджер видит всё, что завёл). Кэш витрины здесь не используется — админка читает
/// свежие данные сразу после собственных изменений.
/// </summary>
public sealed record GetAdminProductsQuery(
    ProductStatus? Status = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ProductSummaryDto>>;

internal sealed class GetAdminProductsQueryHandler(IProductRepository products)
    : IRequestHandler<GetAdminProductsQuery, PagedResult<ProductSummaryDto>>
{
    public async Task<PagedResult<ProductSummaryDto>> Handle(
        GetAdminProductsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new ProductListFilter(
            CategoryId: null,
            SolventType: null,
            Search: request.Search,
            OnlyPublished: false,
            Page: request.Page,
            PageSize: request.PageSize,
            Sort: null,
            Status: request.Status);

        var (items, totalCount) = await products.ListAsync(filter, cancellationToken);

        var dtos = items
            .Select(product => product.Adapt<ProductSummaryDto>())
            .ToList();

        return PagedResult<ProductSummaryDto>.Create(
            dtos,
            totalCount,
            filter.NormalizedPage,
            filter.NormalizedPageSize);
    }
}

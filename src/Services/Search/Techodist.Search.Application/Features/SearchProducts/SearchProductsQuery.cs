using Techodist.BuildingBlocks.Core.Pagination;
using Techodist.Search.Application.Abstractions;
using Techodist.Search.Application.Common;
using Techodist.Search.Application.Dtos;
using Techodist.Search.Application.Models;
using Mapster;
using MediatR;

namespace Techodist.Search.Application.Features.SearchProducts;

/// <summary>
/// Поиск товаров для витрины: полнотекстовый запрос, категория, диапазон цен, сортировка,
/// пагинация. Онлайн-оплаты нет, поиск публичный — токен не нужен (ADR 0004).
/// </summary>
public sealed record SearchProductsQuery(
    string? Q = null,
    Guid? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Sort = null,
    int Page = 1,
    int PageSize = ProductSearchFilter.DefaultPageSize) : IRequest<PagedResult<ProductSearchHitDto>>;

internal sealed class SearchProductsQueryHandler(IProductIndex index)
    : IRequestHandler<SearchProductsQuery, PagedResult<ProductSearchHitDto>>
{
    public async Task<PagedResult<ProductSearchHitDto>> Handle(
        SearchProductsQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new ProductSearchFilter(
            request.Q,
            request.CategoryId,
            request.MinPrice,
            request.MaxPrice,
            ProductSortExtensions.Parse(request.Sort),
            request.Page,
            request.PageSize);

        if (filter.HasInvertedPriceRange)
        {
            // min > max: по определению пустая выдача, поэтому в Elasticsearch не идём.
            return PagedResult<ProductSearchHitDto>.Empty(filter.NormalizedPage, filter.NormalizedPageSize);
        }

        var page = await index.SearchAsync(filter, cancellationToken);

        return PagedResult<ProductSearchHitDto>.Create(
            page.Items.Select(document => document.Adapt<ProductSearchHitDto>()).ToList(),
            page.TotalCount,
            filter.NormalizedPage,
            filter.NormalizedPageSize);
    }
}

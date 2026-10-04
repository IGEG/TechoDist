using MediatR;
using Techodist.BuildingBlocks.Core.Pagination;
using Techodist.Order.Application.Abstractions;
using Techodist.Order.Application.Common;
using Techodist.Order.Application.Dtos;
using Techodist.Order.Application.Models;
using Techodist.Order.Domain.Enums;

namespace Techodist.Order.Application.Features.Orders.Queries.GetOrders;

/// <summary>Постраничный список заявок для админки: фильтр по статусу и поиск по номеру/клиенту.</summary>
public sealed record GetOrdersQuery(
    OrderStatus? Status = null,
    string? Search = null,
    int Page = 1,
    int PageSize = OrderListFilter.DefaultPageSize) : IRequest<PagedResult<OrderSummaryDto>>;

internal sealed class GetOrdersQueryHandler(IOrderRepository orders)
    : IRequestHandler<GetOrdersQuery, PagedResult<OrderSummaryDto>>
{
    public async Task<PagedResult<OrderSummaryDto>> Handle(
        GetOrdersQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new OrderListFilter(request.Status, request.Search, request.Page, request.PageSize);

        var (items, totalCount) = await orders.ListAsync(filter, cancellationToken);

        var dtos = items.Select(order => order.ToSummaryDto()).ToList();

        return PagedResult<OrderSummaryDto>.Create(
            dtos,
            totalCount,
            filter.NormalizedPage,
            filter.NormalizedPageSize);
    }
}

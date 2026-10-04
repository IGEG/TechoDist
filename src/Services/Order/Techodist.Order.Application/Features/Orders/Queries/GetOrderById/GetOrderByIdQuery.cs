using MediatR;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.Order.Application.Abstractions;
using Techodist.Order.Application.Common;
using Techodist.Order.Application.Dtos;

namespace Techodist.Order.Application.Features.Orders.Queries.GetOrderById;

/// <summary>Карточка заявки для админки (позиции и комментарии менеджера включены).</summary>
public sealed record GetOrderByIdQuery(Guid OrderId) : IRequest<Result<OrderDto>>;

internal sealed class GetOrderByIdQueryHandler(IOrderRepository orders)
    : IRequestHandler<GetOrderByIdQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken);

        return order is null
            ? Result.Failure<OrderDto>(Error.NotFound(
                "order.not_found",
                $"Заявка '{request.OrderId}' не найдена."))
            : order.ToDto();
    }
}

using MediatR;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.Order.Application.Abstractions;
using Techodist.Order.Application.Common;
using Techodist.Order.Application.Dtos;
using Techodist.Order.Domain.ValueObjects;

namespace Techodist.Order.Application.Features.Orders.Queries.GetOrderByNumber;

/// <summary>
/// Проверка статуса по читаемому номеру: гость не аутентифицируется, а номер знает из письма.
/// Внутренний GUID наружу для этого сценария не нужен.
/// </summary>
public sealed record GetOrderByNumberQuery(string Number) : IRequest<Result<OrderDto>>;

internal sealed class GetOrderByNumberQueryHandler(IOrderRepository orders)
    : IRequestHandler<GetOrderByNumberQuery, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(
        GetOrderByNumberQuery request,
        CancellationToken cancellationToken)
    {
        var number = OrderNumber.Parse(request.Number);

        if (number.IsFailure)
        {
            return Result.Failure<OrderDto>(number.Error);
        }

        var order = await orders.GetByNumberAsync(number.Value, cancellationToken);

        return order is null
            ? Result.Failure<OrderDto>(Error.NotFound(
                "order.not_found",
                $"Заявка с номером '{number.Value.Value}' не найдена."))
            : order.ToDto();
    }
}

using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Techodist.BuildingBlocks.Core.Diagnostics;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.Order.Application.Abstractions;
using Techodist.Order.Application.Common;
using Techodist.Order.Application.Dtos;
using Techodist.Order.Domain.Enums;

namespace Techodist.Order.Application.Features.Orders.Commands.ChangeOrderStatus;

/// <summary>
/// Смена статуса заявки менеджером (админ-панель). Допустимые переходы проверяет агрегат,
/// а факт изменения уходит клиенту письмом через событие (ADR 0003).
/// </summary>
public sealed record ChangeOrderStatusCommand(
    Guid OrderId,
    OrderStatus NewStatus,
    string? ManagerComment = null) : IRequest<Result<OrderDto>>;

internal sealed class ChangeOrderStatusCommandHandler(
    IOrderRepository orders,
    IPublishEndpoint publishEndpoint,
    ILogger<ChangeOrderStatusCommandHandler> logger)
    : IRequestHandler<ChangeOrderStatusCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(
        ChangeOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        using var activity = TechodistDiagnostics.StartActivity(TechodistDiagnostics.ActivityNames.OrderStatusChange);

        activity?.SetTag("order.id", request.OrderId);
        activity?.SetTag("order.to_status", request.NewStatus.ToString());

        var order = await orders.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderDto>(Error.NotFound(
                "order.not_found",
                $"Заявка '{request.OrderId}' не найдена."));
        }

        var oldStatus = order.Status;
        var changed = order.ChangeStatus(request.NewStatus, request.ManagerComment);

        if (changed.IsFailure)
        {
            return Result.Failure<OrderDto>(changed.Error);
        }

        await publishEndpoint.Publish(
            order.ToStatusChangedEvent(oldStatus, request.ManagerComment),
            cancellationToken);

        await orders.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Заявка {OrderNumber} переведена из {OldStatus} в {NewStatus}.",
            order.Number.Value,
            oldStatus,
            order.Status);

        activity?.SetTag("order.number", order.Number.Value);
        activity?.SetTag("order.status", order.Status.ToString());

        TechodistDiagnostics.OrderStatusChanged(oldStatus.ToString(), order.Status.ToString());

        return order.ToDto();
    }
}

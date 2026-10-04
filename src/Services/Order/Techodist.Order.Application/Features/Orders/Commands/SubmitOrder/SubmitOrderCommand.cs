using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.Order.Application.Abstractions;
using Techodist.Order.Application.Common;
using Techodist.Order.Application.Dtos;
using Techodist.Order.Domain.Entities;
using Techodist.Order.Domain.Enums;
using Techodist.Order.Domain.ValueObjects;

namespace Techodist.Order.Application.Features.Orders.Commands.SubmitOrder;

/// <summary>
/// Оформление заявки гостем (онлайн-оплаты нет). Позиции берутся из корзины по <c>basketId</c>
/// из анонимного cookie (ADR 0005), событие уходит в RabbitMQ через outbox (ADR 0003).
/// </summary>
public sealed record SubmitOrderCommand(
    Guid BasketId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone = null,
    string? Comment = null,
    OrderContactChannel PreferredChannel = OrderContactChannel.Email,
    OrderPriority Priority = OrderPriority.Standard) : IRequest<Result<OrderDto>>;

internal sealed class SubmitOrderCommandHandler(
    IOrderRepository orders,
    IBasketClient basket,
    IOrderNumberGenerator numbers,
    IPublishEndpoint publishEndpoint,
    ILogger<SubmitOrderCommandHandler> logger)
    : IRequestHandler<SubmitOrderCommand, Result<OrderDto>>
{
    public async Task<Result<OrderDto>> Handle(
        SubmitOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (request.BasketId == Guid.Empty)
        {
            return Result.Failure<OrderDto>(Error.Validation(
                "order.basket.missing",
                "Корзина не найдена: оформите заявку из корзины на витрине."));
        }

        var snapshot = await basket.GetBasketAsync(request.BasketId, cancellationToken);

        if (snapshot is null || snapshot.IsEmpty)
        {
            return Result.Failure<OrderDto>(Error.Validation(
                "order.basket.empty",
                "Корзина пуста — добавьте товары перед оформлением заявки."));
        }

        var number = await numbers.NextAsync(DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);

        var order = OrderAggregate.Create(
            number,
            request.CustomerName,
            request.CustomerEmail,
            snapshot.Items.Select(item => OrderItem.Create(
                item.ProductId,
                item.ProductName,
                item.ImageUrl,
                Money.Create(item.UnitPrice, item.Currency),
                item.Quantity)),
            request.CustomerPhone,
            request.Comment,
            request.PreferredChannel,
            request.Priority,
            request.BasketId);

        await orders.AddAsync(order, cancellationToken);

        // Событие публикуется ДО SaveChanges: bus outbox MassTransit положит его в таблицу outbox
        // в той же транзакции, что и заявку, поэтому «заказ без события» невозможен (ADR 0003).
        await publishEndpoint.Publish(order.ToSubmittedEvent(), cancellationToken);
        await orders.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Оформлена заявка {OrderNumber} ({OrderId}) на сумму {TotalAmount} {Currency}.",
            order.Number.Value,
            order.Id,
            order.TotalAmount,
            order.Currency);

        await TryClearBasketAsync(request.BasketId, cancellationToken);

        return order.ToDto();
    }

    /// <summary>
    /// Корзина очищается best-effort: заявка уже сохранена и событие в outbox, поэтому недоступность
    /// Basket не должна превращать успешное оформление в ошибку (гость получит письмо, а корзину
    /// можно очистить и вручную).
    /// </summary>
    private async Task TryClearBasketAsync(Guid basketId, CancellationToken cancellationToken)
    {
        try
        {
            await basket.ClearBasketAsync(basketId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Не удалось очистить корзину {BasketId} после оформления заявки.",
                basketId);
        }
    }
}

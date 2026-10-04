using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.BuildingBlocks.Web.Extensions;
using Techodist.Order.Api.Contracts;
using Techodist.Order.Api.Orders;
using Techodist.Order.Application.Dtos;
using Techodist.Order.Application.Features.Orders.Commands.ChangeOrderStatus;
using Techodist.Order.Application.Features.Orders.Commands.SubmitOrder;
using Techodist.Order.Application.Features.Orders.Queries.GetOrderById;
using Techodist.Order.Application.Features.Orders.Queries.GetOrderByNumber;
using Techodist.Order.Application.Features.Orders.Queries.GetOrders;

namespace Techodist.Order.Api.Controllers;

/// <summary>
/// Заявки магазина. Гость (витрина) умеет только оформить заявку из своей корзины и проверить
/// её статус по номеру; всё остальное — работа менеджера и требует токен админ-панели.
/// </summary>
[ApiController]
[Route("api/orders")]
public sealed class OrdersController(ISender sender, BasketIdReader basketIds) : ControllerBase
{
    /// <summary>
    /// Оформление заявки гостем. Корзина берётся по анонимному cookie, покупатель не аутентифицируется
    /// (ADR 0005); письма магазину и клиенту отправит Notification по событию из outbox.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Submit(
        [FromBody] SubmitOrderRequest request,
        CancellationToken cancellationToken)
    {
        var basketId = basketIds.Read();

        if (basketId is null)
        {
            return Error.Validation(
                    "order.basket.missing",
                    "Корзина не найдена: оформите заявку со страницы корзины.")
                .ToProblemResult();
        }

        var command = new SubmitOrderCommand(
            basketId.Value,
            request.CustomerName,
            request.CustomerEmail,
            request.CustomerPhone,
            request.Comment,
            request.PreferredChannel,
            request.Priority);

        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetByNumber), new { number = result.Value.Number }, result.Value)
            : result.Error.ToProblemResult();
    }

    /// <summary>Проверка статуса заявки по номеру из письма (гостю доступна без токена).</summary>
    [HttpGet("number/{number}")]
    [ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByNumber(string number, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrderByNumberQuery(number), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.Error.ToProblemResult();
    }

    /// <summary>Постраничный список заявок для админ-панели.</summary>
    [HttpGet]
    [Authorize(Roles = AuthenticationExtensions.AdminRoles)]
    public async Task<IActionResult> List(
        [FromQuery] GetOrdersQuery query,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(query, cancellationToken));

    /// <summary>Карточка заявки для админ-панели.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = AuthenticationExtensions.AdminRoles)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetOrderByIdQuery(id), cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.Error.ToProblemResult();
    }

    /// <summary>Смена статуса заявки менеджером (клиент узнаёт об этом письмом).</summary>
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = AuthenticationExtensions.AdminRoles)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        [FromBody] ChangeOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeOrderStatusCommand(id, request.Status, request.ManagerComment);
        var result = await sender.Send(command, cancellationToken);

        return result.IsSuccess ? Ok(result.Value) : result.Error.ToProblemResult();
    }
}

using Techodist.Basket.Api.Baskets;
using Techodist.Basket.Api.Contracts;
using Techodist.Basket.Application.Dtos;
using Techodist.Basket.Application.Features.Baskets.Commands.AddBasketItem;
using Techodist.Basket.Application.Features.Baskets.Commands.ClearBasket;
using Techodist.Basket.Application.Features.Baskets.Commands.RemoveBasketItem;
using Techodist.Basket.Application.Features.Baskets.Commands.UpdateBasketItemQuantity;
using Techodist.Basket.Application.Features.Baskets.Queries.GetBasket;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.BuildingBlocks.Web.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Techodist.Basket.Api.Controllers;

/// <summary>
/// Гостевая корзина (ADR 0005). Покупатель не аутентифицируется: корзина привязана к анонимному
/// HttpOnly-cookie, поэтому на маршрутах нет <c>[Authorize]</c> — защищать их нечем и незачем.
/// </summary>
[ApiController]
[Route("api/basket")]
public sealed class BasketController(ISender sender, BasketIdProvider basketIds) : ControllerBase
{
    /// <summary>Текущая корзина гостя: пустой объект, если корзины ещё нет.</summary>
    [HttpGet]
    public async Task<ActionResult<BasketDto>> Get(CancellationToken cancellationToken)
        => await sender.Send(new GetBasketQuery(basketIds.GetOrCreateBasketId()), cancellationToken);

    /// <summary>Добавление товара (цена и название фиксируются снимком из Catalog).</summary>
    [HttpPost("items")]
    public async Task<IActionResult> AddItem(
        [FromBody] AddBasketItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddBasketItemCommand(
            basketIds.GetOrCreateBasketId(),
            request.ProductId,
            request.Quantity);

        return ToActionResult(await sender.Send(command, cancellationToken));
    }

    /// <summary>Изменение количества позиции.</summary>
    [HttpPut("items/{productId:guid}")]
    public async Task<IActionResult> UpdateItem(
        Guid productId,
        [FromBody] UpdateBasketItemRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateBasketItemQuantityCommand(
            basketIds.GetOrCreateBasketId(),
            productId,
            request.Quantity);

        return ToActionResult(await sender.Send(command, cancellationToken));
    }

    /// <summary>Удаление позиции из корзины.</summary>
    [HttpDelete("items/{productId:guid}")]
    public async Task<IActionResult> RemoveItem(Guid productId, CancellationToken cancellationToken)
    {
        var command = new RemoveBasketItemCommand(basketIds.GetOrCreateBasketId(), productId);

        return ToActionResult(await sender.Send(command, cancellationToken));
    }

    /// <summary>Полная очистка корзины.</summary>
    [HttpDelete]
    public async Task<ActionResult<BasketDto>> Clear(CancellationToken cancellationToken)
        => await sender.Send(new ClearBasketCommand(basketIds.GetOrCreateBasketId()), cancellationToken);

    private IActionResult ToActionResult(Result<BasketDto> result)
        => result.IsSuccess ? Ok(result.Value) : result.Error.ToProblemResult();
}

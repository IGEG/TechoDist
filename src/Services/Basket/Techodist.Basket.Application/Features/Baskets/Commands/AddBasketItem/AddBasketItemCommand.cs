using Techodist.Basket.Application.Abstractions;
using Techodist.Basket.Application.Common;
using Techodist.Basket.Application.Dtos;
using Techodist.Basket.Domain.Entities;
using Techodist.Basket.Domain.ValueObjects;
using Techodist.BuildingBlocks.Core.Results;
using MediatR;

namespace Techodist.Basket.Application.Features.Baskets.Commands.AddBasketItem;

/// <summary>
/// Добавление товара в корзину. Снимок товара (название, картинка, цена) берётся из Catalog
/// синхронным вызовом и сохраняется в позиции (ADR 0005).
/// </summary>
public sealed record AddBasketItemCommand(Guid BasketId, Guid ProductId, int Quantity = 1)
    : IRequest<Result<BasketDto>>;

internal sealed class AddBasketItemCommandHandler(
    IBasketRepository baskets,
    ICatalogProductClient catalog)
    : IRequestHandler<AddBasketItemCommand, Result<BasketDto>>
{
    public async Task<Result<BasketDto>> Handle(
        AddBasketItemCommand request,
        CancellationToken cancellationToken)
    {
        var product = await catalog.GetProductAsync(request.ProductId, cancellationToken);

        if (product is null)
        {
            return Result.Failure<BasketDto>(Error.NotFound(
                "basket.product.not_found",
                $"Товар '{request.ProductId}' не найден в каталоге."));
        }

        if (!product.IsAvailable)
        {
            return Result.Failure<BasketDto>(Error.Validation(
                "basket.product.unavailable",
                $"Товар '{product.Name}' недоступен для заказа."));
        }

        // Корзины ещё нет (первое обращение гостя) — создаём с идентификатором из cookie,
        // поэтому basketId в cookie и в Redis всегда совпадают.
        var basket = await baskets.GetAsync(request.BasketId, cancellationToken)
            ?? ShoppingBasket.Create(request.BasketId);

        var added = basket.AddItem(
            product.Id,
            product.Name,
            product.ImageUrl,
            Money.Create(product.Price, product.Currency),
            request.Quantity);

        if (added.IsFailure)
        {
            return Result.Failure<BasketDto>(added.Error);
        }

        await baskets.SaveAsync(basket, cancellationToken);

        return basket.ToDto();
    }
}

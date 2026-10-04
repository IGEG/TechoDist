using Techodist.Basket.Application.Abstractions;
using Techodist.Basket.Application.Common;
using Techodist.Basket.Application.Dtos;
using Techodist.BuildingBlocks.Core.Results;
using MediatR;

namespace Techodist.Basket.Application.Features.Baskets.Commands.RemoveBasketItem;

/// <summary>Удаление позиции из корзины.</summary>
public sealed record RemoveBasketItemCommand(Guid BasketId, Guid ProductId) : IRequest<Result<BasketDto>>;

internal sealed class RemoveBasketItemCommandHandler(IBasketRepository baskets)
    : IRequestHandler<RemoveBasketItemCommand, Result<BasketDto>>
{
    public async Task<Result<BasketDto>> Handle(
        RemoveBasketItemCommand request,
        CancellationToken cancellationToken)
    {
        var basket = await baskets.GetAsync(request.BasketId, cancellationToken);

        if (basket is null)
        {
            return Result.Failure<BasketDto>(Error.NotFound(
                "basket.not_found",
                "Корзина не найдена: она не создавалась или срок хранения истёк."));
        }

        var removed = basket.RemoveItem(request.ProductId);

        if (removed.IsFailure)
        {
            return Result.Failure<BasketDto>(removed.Error);
        }

        // Удаляем последнюю позицию — убираем и саму корзину: не держим в Redis пустые ключи до TTL.
        if (basket.IsEmpty)
        {
            await baskets.DeleteAsync(request.BasketId, cancellationToken);

            return BasketDtoMapper.Empty(request.BasketId);
        }

        await baskets.SaveAsync(basket, cancellationToken);

        return basket.ToDto();
    }
}

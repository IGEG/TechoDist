using Techodist.Basket.Application.Abstractions;
using Techodist.Basket.Application.Common;
using Techodist.Basket.Application.Dtos;
using Techodist.BuildingBlocks.Core.Results;
using MediatR;

namespace Techodist.Basket.Application.Features.Baskets.Commands.UpdateBasketItemQuantity;

/// <summary>Изменение количества товара в корзине (шаг «+1»/«−1» или ручной ввод на витрине).</summary>
public sealed record UpdateBasketItemQuantityCommand(Guid BasketId, Guid ProductId, int Quantity)
    : IRequest<Result<BasketDto>>;

internal sealed class UpdateBasketItemQuantityCommandHandler(IBasketRepository baskets)
    : IRequestHandler<UpdateBasketItemQuantityCommand, Result<BasketDto>>
{
    public async Task<Result<BasketDto>> Handle(
        UpdateBasketItemQuantityCommand request,
        CancellationToken cancellationToken)
    {
        var basket = await baskets.GetAsync(request.BasketId, cancellationToken);

        if (basket is null)
        {
            return Result.Failure<BasketDto>(Error.NotFound(
                "basket.not_found",
                "Корзина не найдена: она не создавалась или срок хранения истёк."));
        }

        var updated = basket.ChangeItemQuantity(request.ProductId, request.Quantity);

        if (updated.IsFailure)
        {
            return Result.Failure<BasketDto>(updated.Error);
        }

        await baskets.SaveAsync(basket, cancellationToken);

        return basket.ToDto();
    }
}

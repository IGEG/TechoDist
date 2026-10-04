using Techodist.Basket.Application.Abstractions;
using Techodist.Basket.Application.Common;
using Techodist.Basket.Application.Dtos;
using MediatR;

namespace Techodist.Basket.Application.Features.Baskets.Commands.ClearBasket;

/// <summary>Полная очистка корзины (идемпотентна: отсутствие корзины — тоже успех).</summary>
public sealed record ClearBasketCommand(Guid BasketId) : IRequest<BasketDto>;

internal sealed class ClearBasketCommandHandler(IBasketRepository baskets)
    : IRequestHandler<ClearBasketCommand, BasketDto>
{
    public async Task<BasketDto> Handle(ClearBasketCommand request, CancellationToken cancellationToken)
    {
        await baskets.DeleteAsync(request.BasketId, cancellationToken);

        return BasketDtoMapper.Empty(request.BasketId);
    }
}

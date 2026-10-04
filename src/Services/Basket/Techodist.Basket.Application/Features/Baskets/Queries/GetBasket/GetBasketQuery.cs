using Techodist.Basket.Application.Abstractions;
using Techodist.Basket.Application.Common;
using Techodist.Basket.Application.Dtos;
using MediatR;

namespace Techodist.Basket.Application.Features.Baskets.Queries.GetBasket;

/// <summary>Чтение корзины гостя. Идентификатор приходит из анонимного cookie (ADR 0005).</summary>
public sealed record GetBasketQuery(Guid BasketId) : IRequest<BasketDto>;

internal sealed class GetBasketQueryHandler(IBasketRepository baskets)
    : IRequestHandler<GetBasketQuery, BasketDto>
{
    public async Task<BasketDto> Handle(GetBasketQuery request, CancellationToken cancellationToken)
    {
        var basket = await baskets.GetAsync(request.BasketId, cancellationToken);

        return basket is null
            ? BasketDtoMapper.Empty(request.BasketId)
            : basket.ToDto();
    }
}

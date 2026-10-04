using EcoTech.BuildingBlocks.Core.Results;
using EcoTech.Catalog.Application.Abstractions;
using EcoTech.Catalog.Application.Dtos;
using Mapster;
using MediatR;

namespace EcoTech.Catalog.Application.Features.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDetailsDto>>;

internal sealed class GetProductByIdQueryHandler(IProductRepository products)
    : IRequestHandler<GetProductByIdQuery, Result<ProductDetailsDto>>
{
    public async Task<Result<ProductDetailsDto>> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
        {
            return Result.Failure<ProductDetailsDto>(
                Error.NotFound("catalog.product.not_found", $"Товар с идентификатором '{request.Id}' не найден."));
        }

        return product.Adapt<ProductDetailsDto>();
    }
}

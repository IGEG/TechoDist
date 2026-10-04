using Techodist.BuildingBlocks.Core.Diagnostics;
using Techodist.BuildingBlocks.Core.Results;
using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Catalog.Application.Abstractions;
using Techodist.Catalog.Application.Common;
using Techodist.Catalog.Domain.Enums;
using MassTransit;
using MediatR;

namespace Techodist.Catalog.Application.Features.Products.Commands.PublishProduct;

/// <summary>
/// Публикация товара: карточка появляется на витрине и в поисковом индексе (ADR 0009).
/// </summary>
public sealed record PublishProductCommand(Guid ProductId) : IRequest<Result<Guid>>;

internal sealed class PublishProductCommandHandler(
    IProductRepository products,
    ICategoryRepository categories,
    IPublishEndpoint publishEndpoint,
    CatalogCacheInvalidator cache)
    : IRequestHandler<PublishProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(PublishProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<Guid>(
                Error.NotFound("catalog.product.not_found", $"Товар с идентификатором '{request.ProductId}' не найден."));
        }

        if (product.Status == ProductStatus.Published)
        {
            return Result.Failure<Guid>(
                Error.Conflict("catalog.product.already_published", $"Товар '{product.Name}' уже опубликован."));
        }

        var category = await categories.GetByIdAsync(product.CategoryId, cancellationToken);

        product.Publish();
        products.Update(product);

        await publishEndpoint.Publish(
            product.ToIntegrationEvent(category?.Name, ProductChangeType.Updated),
            cancellationToken);

        await products.SaveChangesAsync(cancellationToken);

        await cache.InvalidateProductsAsync(cancellationToken);

        TechodistDiagnostics.ProductPublished();

        return product.Id;
    }
}

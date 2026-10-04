using Techodist.BuildingBlocks.Core.Results;
using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Catalog.Application.Abstractions;
using Techodist.Catalog.Application.Common;
using Techodist.Catalog.Domain.Enums;
using MassTransit;
using MediatR;

namespace Techodist.Catalog.Application.Features.Products.Commands.ArchiveProduct;

/// <summary>
/// Снятие товара с продажи (архив). История сохраняется: на позиции ссылаются заявки, поэтому
/// физического удаления опубликованного товара нет — с витрины и из индекса поиска карточка
/// исчезает по признаку «не опубликован» (ADR 0009).
/// </summary>
public sealed record ArchiveProductCommand(Guid ProductId) : IRequest<Result<Guid>>;

internal sealed class ArchiveProductCommandHandler(
    IProductRepository products,
    ICategoryRepository categories,
    IPublishEndpoint publishEndpoint,
    CatalogCacheInvalidator cache)
    : IRequestHandler<ArchiveProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ArchiveProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<Guid>(
                Error.NotFound("catalog.product.not_found", $"Товар с идентификатором '{request.ProductId}' не найден."));
        }

        if (product.Status == ProductStatus.Archived)
        {
            return Result.Failure<Guid>(
                Error.Conflict("catalog.product.already_archived", $"Товар '{product.Name}' уже снят с продажи."));
        }

        var category = await categories.GetByIdAsync(product.CategoryId, cancellationToken);

        product.Archive();
        products.Update(product);

        await publishEndpoint.Publish(
            product.ToIntegrationEvent(category?.Name, ProductChangeType.Updated),
            cancellationToken);

        await products.SaveChangesAsync(cancellationToken);

        await cache.InvalidateProductsAsync(cancellationToken);

        return product.Id;
    }
}

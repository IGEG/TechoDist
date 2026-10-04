using Techodist.BuildingBlocks.Core.Results;
using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Catalog.Application.Abstractions;
using Techodist.Catalog.Application.Common;
using Techodist.Catalog.Domain.Enums;
using MassTransit;
using MediatR;

namespace Techodist.Catalog.Application.Features.Products.Commands.DeleteProduct;

/// <summary>
/// Физическое удаление товара разрешено только для черновика: он никогда не был на витрине,
/// и на него не могли сослаться заявки. Опубликованный товар снимают с продажи (архив) —
/// иначе у заявок остались бы ссылки на исчезнувшую позицию.
/// </summary>
public sealed record DeleteProductCommand(Guid ProductId) : IRequest<Result<Guid>>;

internal sealed class DeleteProductCommandHandler(
    IProductRepository products,
    ICategoryRepository categories,
    IPublishEndpoint publishEndpoint,
    CatalogCacheInvalidator cache)
    : IRequestHandler<DeleteProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<Guid>(
                Error.NotFound("catalog.product.not_found", $"Товар с идентификатором '{request.ProductId}' не найден."));
        }

        if (product.Status != ProductStatus.Draft)
        {
            return Result.Failure<Guid>(Error.Conflict(
                "catalog.product.delete_forbidden",
                $"Товар '{product.Name}' нельзя удалить: он был опубликован. Снимите его с продажи (архив) — заявки ссылаются на позицию."));
        }

        var category = await categories.GetByIdAsync(product.CategoryId, cancellationToken);

        products.Remove(product);

        // Для Deleted-события поиску достаточно идентификатора: он удаляет документ индекса.
        await publishEndpoint.Publish(
            product.ToIntegrationEvent(category?.Name, ProductChangeType.Deleted),
            cancellationToken);

        await products.SaveChangesAsync(cancellationToken);

        await cache.InvalidateProductsAsync(cancellationToken);

        return product.Id;
    }
}

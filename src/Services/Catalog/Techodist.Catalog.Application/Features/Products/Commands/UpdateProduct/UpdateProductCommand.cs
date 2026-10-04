using Techodist.BuildingBlocks.Core.Results;
using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Catalog.Application.Abstractions;
using Techodist.Catalog.Application.Common;
using Techodist.Catalog.Domain.ValueObjects;
using MassTransit;
using MediatR;

namespace Techodist.Catalog.Application.Features.Products.Commands.UpdateProduct;

/// <summary>
/// Изменение товара админ-панелью. Событие <c>ProductChanged</c> уходит через outbox в той же
/// транзакции, что и запись товара, поэтому индекс поиска обновляется вслед за каталогом (ADR 0003, 0009).
/// </summary>
public sealed record UpdateProductCommand(
    Guid ProductId,
    string Name,
    Guid CategoryId,
    decimal Price,
    string? ShortDescription = null,
    string? Description = null,
    string? SolventType = null,
    int? VolumeLiters = null,
    string? Slug = null) : IRequest<Result<Guid>>;

internal sealed class UpdateProductCommandHandler(
    IProductRepository products,
    ICategoryRepository categories,
    IPublishEndpoint publishEndpoint,
    CatalogCacheInvalidator cache)
    : IRequestHandler<UpdateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Failure<Guid>(
                Error.NotFound("catalog.product.not_found", $"Товар с идентификатором '{request.ProductId}' не найден."));
        }

        var category = await categories.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure<Guid>(
                Error.Validation("catalog.category.not_found", $"Категория '{request.CategoryId}' не найдена."));
        }

        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            var slug = Slug.Create(request.Slug).Value;

            if (await products.SlugExistsAsync(slug, product.Id, cancellationToken))
            {
                return Result.Failure<Guid>(
                    Error.Conflict("catalog.product.slug_exists", $"Slug '{slug}' уже используется другим товаром."));
            }
        }

        product.Update(
            request.Name,
            request.CategoryId,
            Money.Rub(request.Price),
            request.ShortDescription,
            request.Description,
            request.SolventType,
            request.VolumeLiters,
            request.Slug);

        products.Update(product);

        await publishEndpoint.Publish(
            product.ToIntegrationEvent(category.Name, ProductChangeType.Updated),
            cancellationToken);

        await products.SaveChangesAsync(cancellationToken);

        await cache.InvalidateProductsAsync(cancellationToken);

        return product.Id;
    }
}

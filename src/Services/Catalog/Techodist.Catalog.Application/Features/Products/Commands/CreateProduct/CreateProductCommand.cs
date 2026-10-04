using Techodist.BuildingBlocks.Core.Results;
using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Catalog.Application.Abstractions;
using Techodist.Catalog.Application.Common;
using Techodist.Catalog.Domain.Entities;
using Techodist.Catalog.Domain.ValueObjects;
using MassTransit;
using MediatR;

namespace Techodist.Catalog.Application.Features.Products.Commands.CreateProduct;

public sealed record CreateProductCommand(
    string Name,
    Guid CategoryId,
    decimal Price,
    string? ShortDescription = null,
    string? Description = null,
    string? SolventType = null,
    int? VolumeLiters = null,
    string? Slug = null) : IRequest<Result<Guid>>;

internal sealed class CreateProductCommandHandler(
    IProductRepository products,
    ICategoryRepository categories,
    IPublishEndpoint publishEndpoint,
    CatalogCacheInvalidator cache)
    : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var category = await categories.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null)
        {
            return Result.Failure<Guid>(
                Error.Validation("catalog.category.not_found", $"Категория '{request.CategoryId}' не найдена."));
        }

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slug.FromName(request.Name).Value
            : Slug.Create(request.Slug).Value;

        if (await products.SlugExistsAsync(slug, null, cancellationToken))
        {
            return Result.Failure<Guid>(
                Error.Conflict("catalog.product.slug_exists", $"Slug '{slug}' уже используется другим товаром."));
        }

        var product = Product.Create(
            request.Name,
            request.CategoryId,
            Money.Rub(request.Price),
            request.ShortDescription,
            request.Description,
            request.SolventType,
            request.VolumeLiters,
            slug);

        await products.AddAsync(product, cancellationToken);

        // Событие для Search публикуется ДО SaveChanges: bus outbox MassTransit запишет его
        // в ту же транзакцию, что и товар, поэтому «товар без события» невозможен (ADR 0003).
        await publishEndpoint.Publish(
            product.ToIntegrationEvent(category.Name, ProductChangeType.Created),
            cancellationToken);

        await products.SaveChangesAsync(cancellationToken);

        await cache.InvalidateProductsAsync(cancellationToken);

        return product.Id;
    }
}

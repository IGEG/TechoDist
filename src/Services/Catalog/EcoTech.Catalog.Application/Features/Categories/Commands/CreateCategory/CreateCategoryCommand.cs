using EcoTech.BuildingBlocks.Core.Results;
using EcoTech.Catalog.Application.Abstractions;
using EcoTech.Catalog.Domain.Entities;
using EcoTech.Catalog.Domain.ValueObjects;
using MediatR;

namespace EcoTech.Catalog.Application.Features.Categories.Commands.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string? Description = null,
    Guid? ParentId = null,
    int SortOrder = 0,
    string? Slug = null) : IRequest<Result<Guid>>;

internal sealed class CreateCategoryCommandHandler(ICategoryRepository categories)
    : IRequestHandler<CreateCategoryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (await categories.NameExistsAsync(request.Name, cancellationToken))
        {
            return Result.Failure<Guid>(
                Error.Conflict("catalog.category.name_exists", $"Категория '{request.Name}' уже существует."));
        }

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? Slug.FromName(request.Name)
            : Slug.Create(request.Slug);

        if (await categories.SlugExistsAsync(slug.Value, null, cancellationToken))
        {
            return Result.Failure<Guid>(
                Error.Conflict("catalog.category.slug_exists", $"Slug '{slug}' уже используется."));
        }

        var category = Category.Create(
            request.Name,
            request.Description,
            request.ParentId,
            request.SortOrder,
            slug.Value);

        await categories.AddAsync(category, cancellationToken);
        await categories.SaveChangesAsync(cancellationToken);

        return category.Id;
    }
}

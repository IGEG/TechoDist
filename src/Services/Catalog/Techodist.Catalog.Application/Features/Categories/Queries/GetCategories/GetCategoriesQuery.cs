using Techodist.Catalog.Application.Abstractions;
using Techodist.Catalog.Application.Dtos;
using Mapster;
using MediatR;

namespace Techodist.Catalog.Application.Features.Categories.Queries.GetCategories;

public sealed record GetCategoriesQuery(bool OnlyPublished = true) : IRequest<IReadOnlyList<CategoryDto>>;

internal sealed class GetCategoriesQueryHandler(ICategoryRepository categories)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var items = await categories.ListAsync(request.OnlyPublished, cancellationToken);

        return items
            .Select(category => category.Adapt<CategoryDto>())
            .ToList();
    }
}

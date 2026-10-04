using Techodist.BuildingBlocks.Core.Pagination;
using Techodist.Catalog.Application.Common;
using Techodist.Catalog.Application.Dtos;
using Techodist.Catalog.Application.Features.Products.Queries.GetAdminProducts;
using Techodist.Catalog.Domain.Entities;
using Techodist.Catalog.Domain.Enums;
using Techodist.Catalog.Domain.ValueObjects;
using Techodist.Catalog.UnitTests.Fakes;
using Mapster;
using Xunit;

namespace Techodist.Catalog.UnitTests.Application;

/// <summary>
/// Админский список товаров: он обязан показывать черновики и архив (в отличие от витрины),
/// иначе менеджер не увидит только что созданную карточку и не сможет её опубликовать.
/// </summary>
public sealed class GetAdminProductsQueryHandlerTests
{
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly FakeProductRepository _repository = new();

    public GetAdminProductsQueryHandlerTests()
        => CatalogMappingConfig.Register(TypeAdapterConfig.GlobalSettings);

    [Fact]
    public async Task Handle_ReturnsDraftsAndArchived_WhenStatusIsNotSpecified()
    {
        Seed(ProductStatus.Draft, "Черновик TD20");
        Seed(ProductStatus.Published, "Опубликованный TD60");
        Seed(ProductStatus.Archived, "Архив TD120");

        var page = await Handle(new GetAdminProductsQuery());

        Assert.Equal(3, page.TotalCount);
        Assert.Contains(page.Items, item => item.Status == nameof(ProductStatus.Draft));
        Assert.Contains(page.Items, item => item.Status == nameof(ProductStatus.Published));
        Assert.Contains(page.Items, item => item.Status == nameof(ProductStatus.Archived));
    }

    [Fact]
    public async Task Handle_FiltersByStatus()
    {
        Seed(ProductStatus.Draft, "Черновик TD20");
        Seed(ProductStatus.Published, "Опубликованный TD60");

        var page = await Handle(new GetAdminProductsQuery(ProductStatus.Draft));

        Assert.Equal(1, page.TotalCount);
        Assert.Equal("Черновик TD20", page.Items[0].Name);
        Assert.Equal(nameof(ProductStatus.Draft), page.Items[0].Status);
    }

    [Fact]
    public async Task Handle_AppliesSearchAndPaging()
    {
        for (var index = 0; index < 3; index++)
        {
            Seed(ProductStatus.Published, $"Установка TD{index}");
        }

        Seed(ProductStatus.Published, "Вакуумная установка TDV40");

        var search = await Handle(new GetAdminProductsQuery(Search: "TDV"));

        Assert.Equal(1, search.TotalCount);
        Assert.Equal("Вакуумная установка TDV40", search.Items[0].Name);

        var firstPage = await Handle(new GetAdminProductsQuery(Page: 1, PageSize: 2));

        Assert.Equal(4, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.True(firstPage.HasNext);
    }

    private Task<PagedResult<ProductSummaryDto>> Handle(GetAdminProductsQuery query)
        => new GetAdminProductsQueryHandler(_repository).Handle(query, CancellationToken.None);

    private void Seed(ProductStatus status, string name)
    {
        var product = Product.Create(name, _categoryId, Money.Rub(1_000m));

        switch (status)
        {
            case ProductStatus.Published:
                product.Publish();
                break;
            case ProductStatus.Archived:
                product.Publish();
                product.Archive();
                break;
            default:
                break;
        }

        _repository.Seed(product);
    }
}

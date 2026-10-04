using EcoTech.BuildingBlocks.Core.Entities;

namespace EcoTech.Catalog.Domain.Entities;

/// <summary>Изображение товара (сущность внутри агрегата Product).</summary>
public sealed class ProductImage : Entity<Guid>
{
    private ProductImage()
    {
    }

    internal ProductImage(Guid id, string url, string? altText, int sortOrder, bool isMain)
        : base(id)
    {
        Url = url;
        AltText = altText;
        SortOrder = sortOrder;
        IsMain = isMain;
    }

    public string Url { get; private set; } = default!;

    public string? AltText { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsMain { get; private set; }

    internal void SetAsMain(bool isMain) => IsMain = isMain;
}

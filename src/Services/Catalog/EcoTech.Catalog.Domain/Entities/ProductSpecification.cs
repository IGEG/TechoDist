using EcoTech.BuildingBlocks.Core.Entities;

namespace EcoTech.Catalog.Domain.Entities;

/// <summary>Характеристика товара (сущность внутри агрегата Product).</summary>
public sealed class ProductSpecification : Entity<Guid>
{
    private ProductSpecification()
    {
    }

    internal ProductSpecification(Guid id, string name, string value, int sortOrder)
        : base(id)
    {
        Name = name;
        Value = value;
        SortOrder = sortOrder;
    }

    public string Name { get; private set; } = default!;

    public string Value { get; private set; } = default!;

    public int SortOrder { get; private set; }
}

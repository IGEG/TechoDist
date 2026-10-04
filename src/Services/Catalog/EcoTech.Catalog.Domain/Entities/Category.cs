using EcoTech.BuildingBlocks.Core.Entities;
using EcoTech.Catalog.Domain.ValueObjects;

namespace EcoTech.Catalog.Domain.Entities;

/// <summary>Категория каталога (например, «Стандартные дистилляторы», «Вакуумные»).</summary>
public sealed class Category : Entity<Guid>, IAggregateRoot
{
    private Category()
    {
    }

    private Category(Guid id, string name, Slug slug, string? description, Guid? parentId, int sortOrder)
        : base(id)
    {
        Name = name;
        Slug = slug;
        Description = description;
        ParentId = parentId;
        SortOrder = sortOrder;
        IsPublished = true;
    }

    public string Name { get; private set; } = default!;

    public Slug Slug { get; private set; } = default!;

    public string? Description { get; private set; }

    public Guid? ParentId { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsPublished { get; private set; }

    public static Category Create(
        string name,
        string? description = null,
        Guid? parentId = null,
        int sortOrder = 0,
        string? slug = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Название категории обязательно.", nameof(name));
        }

        var resolvedSlug = slug is null ? Slug.FromName(name) : Slug.Create(slug);

        return new Category(Guid.NewGuid(), name.Trim(), resolvedSlug, description?.Trim(), parentId, sortOrder);
    }

    public void Update(string name, string? description, Guid? parentId, int sortOrder, string? slug = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Название категории обязательно.", nameof(name));
        }

        Name = name.Trim();
        Description = description?.Trim();
        ParentId = parentId;
        SortOrder = sortOrder;

        if (!string.IsNullOrWhiteSpace(slug))
        {
            Slug = Slug.Create(slug);
        }
    }

    public void Publish() => IsPublished = true;

    public void Unpublish() => IsPublished = false;
}

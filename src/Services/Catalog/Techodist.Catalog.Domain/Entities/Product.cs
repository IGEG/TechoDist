using Techodist.BuildingBlocks.Core.Entities;
using Techodist.Catalog.Domain.Enums;
using Techodist.Catalog.Domain.ValueObjects;

namespace Techodist.Catalog.Domain.Entities;

/// <summary>
/// Товар каталога — установка для очистки/регенерации растворителей.
/// </summary>
public sealed class Product : Entity<Guid>, IAggregateRoot
{
    private readonly List<ProductImage> _images = [];
    private readonly List<ProductSpecification> _specifications = [];

    private Product()
    {
    }

    private Product(Guid id, string name, Slug slug, Guid categoryId, Money price)
        : base(id)
    {
        Name = name;
        Slug = slug;
        CategoryId = categoryId;
        Price = price;
        Status = ProductStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Name { get; private set; } = default!;

    public Slug Slug { get; private set; } = default!;

    public string? ShortDescription { get; private set; }

    public string? Description { get; private set; }

    public Money Price { get; private set; } = default!;

    public Guid CategoryId { get; private set; }

    /// <summary>Марка растворителя (например, «Ацетон», «Толуол»).</summary>
    public string? SolventType { get; private set; }

    /// <summary>Объём установки, литров.</summary>
    public int? VolumeLiters { get; private set; }

    public ProductStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();

    public IReadOnlyCollection<ProductSpecification> Specifications => _specifications.AsReadOnly();

    public static Product Create(
        string name,
        Guid categoryId,
        Money price,
        string? shortDescription = null,
        string? description = null,
        string? solventType = null,
        int? volumeLiters = null,
        string? slug = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Название товара обязательно.", nameof(name));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Категория обязательна.", nameof(categoryId));
        }

        ArgumentNullException.ThrowIfNull(price);

        var resolvedSlug = slug is null ? Slug.FromName(name) : Slug.Create(slug);

        return new Product(Guid.NewGuid(), name.Trim(), resolvedSlug, categoryId, price)
        {
            ShortDescription = shortDescription?.Trim(),
            Description = description?.Trim(),
            SolventType = solventType?.Trim(),
            VolumeLiters = volumeLiters,
        };
    }

    public void Update(
        string name,
        Guid categoryId,
        Money price,
        string? shortDescription,
        string? description,
        string? solventType,
        int? volumeLiters,
        string? slug = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Название товара обязательно.", nameof(name));
        }

        ArgumentNullException.ThrowIfNull(price);

        Name = name.Trim();
        CategoryId = categoryId;
        Price = price;
        ShortDescription = shortDescription?.Trim();
        Description = description?.Trim();
        SolventType = solventType?.Trim();
        VolumeLiters = volumeLiters;
        UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(slug))
        {
            Slug = Slug.Create(slug);
        }
    }

    public void ChangePrice(Money price)
    {
        ArgumentNullException.ThrowIfNull(price);
        Price = price;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Publish()
    {
        Status = ProductStatus.Published;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
        Status = ProductStatus.Archived;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public ProductImage AddImage(string url, string? altText = null, bool isMain = false)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentException("URL изображения обязателен.", nameof(url));
        }

        if (isMain)
        {
            foreach (var existing in _images)
            {
                existing.SetAsMain(false);
            }
        }

        if (_images.Count == 0)
        {
            isMain = true;
        }

        var image = new ProductImage(Guid.NewGuid(), url.Trim(), altText?.Trim(), _images.Count, isMain);
        _images.Add(image);
        return image;
    }

    public ProductSpecification AddSpecification(string name, string value)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Название характеристики обязательно.", nameof(name));
        }

        var specification = new ProductSpecification(Guid.NewGuid(), name.Trim(), value?.Trim() ?? string.Empty, _specifications.Count);
        _specifications.Add(specification);
        return specification;
    }
}

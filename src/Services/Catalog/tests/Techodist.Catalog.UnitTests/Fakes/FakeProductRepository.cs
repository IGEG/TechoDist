using Techodist.Catalog.Application.Abstractions;
using Techodist.Catalog.Application.Models;
using Techodist.Catalog.Domain.Entities;
using Techodist.Catalog.Domain.Enums;

namespace Techodist.Catalog.UnitTests.Fakes;

internal sealed class FakeProductRepository : IProductRepository
{
    private readonly Dictionary<Guid, Product> _store = [];

    public void Seed(params Product[] products)
    {
        foreach (var product in products)
        {
            _store[product.Id] = product;
        }
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.TryGetValue(id, out var product) ? product : null);

    public Task<Product?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.Values.FirstOrDefault(p => p.Slug.Value == slug));

    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.Values.Any(p => p.Slug.Value == slug && (excludeId is null || p.Id != excludeId)));

    public Task<(IReadOnlyList<Product> Items, int TotalCount)> ListAsync(
        ProductListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var query = _store.Values.AsEnumerable();

        // Повторяет семантику EF-репозитория: витрина фильтрует только опубликованные,
        // админский список — по конкретному статусу (черновик/архив).
        if (filter.OnlyPublished)
        {
            query = query.Where(p => p.Status == ProductStatus.Published);
        }
        else if (filter.Status is { } status)
        {
            query = query.Where(p => p.Status == status);
        }

        if (filter.CategoryId is { } categoryId)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();

            query = query.Where(p =>
                p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (p.ShortDescription?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var all = query.OrderBy(p => p.Name).ToList();

        var page = all
            .Skip((filter.NormalizedPage - 1) * filter.NormalizedPageSize)
            .Take(filter.NormalizedPageSize)
            .ToList();

        return Task.FromResult(((IReadOnlyList<Product>)page, all.Count));
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _store[product.Id] = product;
        return Task.CompletedTask;
    }

    public void Update(Product product) => _store[product.Id] = product;

    public void Remove(Product product) => _store.Remove(product.Id);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}

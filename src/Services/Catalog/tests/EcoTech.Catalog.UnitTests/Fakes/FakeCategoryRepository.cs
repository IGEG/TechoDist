using EcoTech.Catalog.Application.Abstractions;
using EcoTech.Catalog.Domain.Entities;

namespace EcoTech.Catalog.UnitTests.Fakes;

internal sealed class FakeCategoryRepository : ICategoryRepository
{
    private readonly Dictionary<Guid, Category> _store = [];

    public void Seed(params Category[] categories)
    {
        foreach (var category in categories)
        {
            _store[category.Id] = category;
        }
    }

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.TryGetValue(id, out var category) ? category : null);

    public Task<IReadOnlyList<Category>> ListAsync(bool onlyPublished = false, CancellationToken cancellationToken = default)
    {
        var items = _store.Values
            .Where(c => !onlyPublished || c.IsPublished)
            .OrderBy(c => c.SortOrder)
            .ToList();

        return Task.FromResult((IReadOnlyList<Category>)items);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.Values.Any(c => c.Slug.Value == slug && (excludeId is null || c.Id != excludeId)));

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.Values.Any(c => c.Name == name));

    public Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        _store[category.Id] = category;
        return Task.CompletedTask;
    }

    public void Update(Category category) => _store[category.Id] = category;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
}

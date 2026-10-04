using Techodist.Search.Application.Abstractions;
using Techodist.Search.Application.Models;

namespace Techodist.Search.UnitTests.Fakes;

/// <summary>Подменный источник каталога: отдаёт заранее заданные снимки товаров и категории.</summary>
internal sealed class FakeCatalogProductSource : ICatalogProductSource
{
    public IReadOnlyList<CatalogProductSnapshot> Products { get; set; } = [];

    public IReadOnlyDictionary<Guid, string> CategoryNames { get; set; } = new Dictionary<Guid, string>();

    public int ListCalls { get; private set; }

    public Task<IReadOnlyList<CatalogProductSnapshot>> ListPublishedProductsAsync(
        CancellationToken cancellationToken = default)
    {
        ListCalls++;

        return Task.FromResult(Products);
    }

    public Task<IReadOnlyDictionary<Guid, string>> GetCategoryNamesAsync(
        CancellationToken cancellationToken = default)
        => Task.FromResult(CategoryNames);
}

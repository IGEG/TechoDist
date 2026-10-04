using Techodist.Search.Application.Abstractions;
using Techodist.Search.Application.Index;
using Techodist.Search.Application.Models;

namespace Techodist.Search.UnitTests.Fakes;

/// <summary>
/// Подменный поисковый индекс: документы лежат в словаре по идентификатору товара, поэтому
/// правила индексации (upsert вместо дубля, удаление снятого товара) проверяются без кластера.
/// </summary>
internal sealed class FakeProductIndex : IProductIndex
{
    private readonly Dictionary<Guid, ProductDocument> _documents = [];
    private readonly List<ProductDocument> _upserts = [];
    private readonly List<Guid> _deleted = [];

    public IReadOnlyDictionary<Guid, ProductDocument> Documents => _documents;

    public IReadOnlyList<ProductDocument> Upserts => _upserts;

    public IReadOnlyList<Guid> Deleted => _deleted;

    /// <summary>Сколько раз индекс готовили к работе (реконсиляция обязана вызвать это до upsert).</summary>
    public int EnsureCalls { get; private set; }

    /// <summary>Доступность кластера для health-check.</summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>Фильтр, дошедший до индекса: <c>null</c> означает, что запрос до индекса не дошёл.</summary>
    public ProductSearchFilter? LastFilter { get; private set; }

    /// <summary>Выдача, которую вернёт поиск (по умолчанию — пустая страница).</summary>
    public ProductIndexPage SearchResult { get; set; } = new([], 0);

    public Task EnsureIndexAsync(CancellationToken cancellationToken = default)
    {
        EnsureCalls++;

        return Task.CompletedTask;
    }

    public Task<ProductIndexPage> SearchAsync(
        ProductSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        LastFilter = filter;

        return Task.FromResult(SearchResult);
    }

    public Task UpsertAsync(ProductDocument document, CancellationToken cancellationToken = default)
    {
        _documents[document.Id] = document;
        _upserts.Add(document);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        _documents.Remove(productId);
        _deleted.Add(productId);

        return Task.CompletedTask;
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(IsAvailable);
}

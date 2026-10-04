using Microsoft.Extensions.Logging.Abstractions;
using Techodist.Search.Application.Features.Reindex;
using Techodist.Search.UnitTests.Fakes;
using Xunit;

namespace Techodist.Search.UnitTests.Application;

/// <summary>
/// Реконсиляция индекса: индекс готовится до upsert (иначе Elasticsearch создал бы его
/// с динамическим маппингом), товары каталога попадают в индекс с именем категории (ADR 0009).
/// </summary>
public sealed class CatalogIndexReconcilerTests
{
    private readonly FakeProductIndex _index = new();

    private readonly FakeCatalogProductSource _catalog = new();

    [Fact]
    public async Task Reconcile_EnsuresIndexBeforeUpsertAndReturnsProductCount()
    {
        _catalog.Products = [SearchTestData.Snapshot()];
        _catalog.CategoryNames = new Dictionary<Guid, string> { [SearchTestData.CategoryId] = SearchTestData.CategoryName };

        var count = await Reconciler().ReconcileAsync(CancellationToken.None);

        Assert.Equal(1, count);
        Assert.Equal(1, _index.EnsureCalls);
        Assert.Equal(1, _catalog.ListCalls);

        var document = Assert.Single(_index.Upserts);

        Assert.Equal(SearchTestData.ProductId, document.Id);
        Assert.Equal(SearchTestData.CategoryName, document.CategoryName);
        Assert.Equal(SearchTestData.Price, document.Price);
        Assert.Equal(SearchTestData.VolumeLiters, document.VolumeLiters);

        // Публичный список каталога отдаёт только опубликованные товары — документ помечен опубликованным.
        Assert.True(document.IsPublished);
    }

    [Fact]
    public async Task Reconcile_UnknownCategory_UpsertsDocumentWithEmptyCategoryName()
    {
        _catalog.Products = [SearchTestData.Snapshot()];

        await Reconciler().ReconcileAsync(CancellationToken.None);

        // Имя категории денормализуется, но его отсутствие не мешает поиску по товару.
        Assert.Equal(string.Empty, Assert.Single(_index.Upserts).CategoryName);
    }

    [Fact]
    public async Task Reconcile_EmptyCatalog_IndexesNothingButStillPreparesIndex()
    {
        var count = await Reconciler().ReconcileAsync(CancellationToken.None);

        Assert.Equal(0, count);
        Assert.Equal(1, _index.EnsureCalls);
        Assert.Empty(_index.Upserts);
    }

    [Fact]
    public async Task Reconcile_DoesNotDeleteExtraDocuments()
    {
        _catalog.Products = [SearchTestData.Snapshot()];

        await Reconciler().ReconcileAsync(CancellationToken.None);

        // Полная пересборка — операция сопровождения (удалить индекс), сверка лишнее не удаляет.
        Assert.Empty(_index.Deleted);
    }

    private CatalogIndexReconciler Reconciler()
        => new(_catalog, _index, NullLogger<CatalogIndexReconciler>.Instance);
}

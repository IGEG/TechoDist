using Microsoft.Extensions.Logging.Abstractions;
using Techodist.BuildingBlocks.Messaging.IntegrationEvents;
using Techodist.Search.Application.Features.ProductChanged;
using Techodist.Search.UnitTests.Fakes;
using Xunit;

namespace Techodist.Search.UnitTests.Application;

/// <summary>
/// Индексация по событию каталога: опубликованный товар попадает в индекс, черновик/архив
/// и удаление убирают документ, повторная доставка сообщения не создаёт дубль (ADR 0009).
/// </summary>
public sealed class ProductChangedIndexerTests
{
    private readonly FakeProductIndex _index = new();

    [Fact]
    public async Task Handle_PublishedProduct_UpsertsDocumentSnapshot()
    {
        await Indexer().HandleAsync(SearchTestData.Published(ProductChangeType.Updated), CancellationToken.None);

        var document = Assert.Single(_index.Upserts);

        Assert.Equal(SearchTestData.ProductId, document.Id);
        Assert.Equal(SearchTestData.ProductName, document.Name);
        Assert.Equal(SearchTestData.ProductSlug, document.Slug);
        Assert.Equal(SearchTestData.SolventType, document.SolventType);
        Assert.Equal(SearchTestData.VolumeLiters, document.VolumeLiters);
        Assert.Equal(SearchTestData.Price, document.Price);
        Assert.Equal("RUB", document.Currency);
        Assert.Equal(SearchTestData.CategoryId, document.CategoryId);
        Assert.Equal(SearchTestData.CategoryName, document.CategoryName);
        Assert.True(document.IsPublished);
        Assert.Equal(SearchTestData.OccurredAt, document.UpdatedAt);
        Assert.Empty(_index.Deleted);
    }

    [Fact]
    public async Task Handle_DuplicateDelivery_UpdatesTheSameDocument()
    {
        var indexer = Indexer();

        await indexer.HandleAsync(SearchTestData.Published(), CancellationToken.None);
        await indexer.HandleAsync(SearchTestData.Published(), CancellationToken.None);

        // Идемпотентность: _id документа — идентификатор товара, поэтому дубля не появляется.
        Assert.Single(_index.Documents);
        Assert.Equal(2, _index.Upserts.Count);
    }

    [Fact]
    public async Task Handle_DraftProduct_RemovesDocumentFromIndex()
    {
        await Indexer().HandleAsync(SearchTestData.Draft(), CancellationToken.None);

        Assert.Equal(SearchTestData.ProductId, Assert.Single(_index.Deleted));
        Assert.Empty(_index.Upserts);
    }

    [Fact]
    public async Task Handle_DeletedProduct_RemovesDocumentFromIndex()
    {
        await Indexer().HandleAsync(SearchTestData.Deleted(), CancellationToken.None);

        Assert.Equal(SearchTestData.ProductId, Assert.Single(_index.Deleted));
        Assert.Empty(_index.Upserts);
    }

    [Fact]
    public async Task Handle_PublishedThenUnpublished_LeavesIndexWithoutDocument()
    {
        var indexer = Indexer();

        await indexer.HandleAsync(SearchTestData.Published(), CancellationToken.None);
        Assert.Single(_index.Documents);

        await indexer.HandleAsync(SearchTestData.Draft(), CancellationToken.None);

        Assert.Empty(_index.Documents);
    }

    [Fact]
    public async Task Handle_NullEvent_IsRejected()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            Indexer().HandleAsync(null!, CancellationToken.None));
    }

    private ProductChangedIndexer Indexer() => new(_index, NullLogger<ProductChangedIndexer>.Instance);
}

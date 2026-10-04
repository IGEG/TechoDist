using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Microsoft.Extensions.Options;
using Techodist.Search.Application.Abstractions;
using Techodist.Search.Application.Index;
using Techodist.Search.Application.Models;

namespace Techodist.Search.Infrastructure.Elasticsearch;

/// <summary>
/// Индекс товаров на Elasticsearch. Транспорт сознательно «тонкий»: правила выдачи (нормализация
/// страниц, разбор сортировки, релевантность) живут в Application и покрыты юнит-тестами,
/// а здесь — только запросы к кластеру и маппинг индекса (ADR 0009).
/// </summary>
internal sealed class ElasticProductIndex(
    ElasticsearchClient client,
    IOptions<ElasticsearchOptions> options) : IProductIndex
{
    private readonly ElasticsearchOptions _options = options.Value;

    public async Task EnsureIndexAsync(CancellationToken cancellationToken = default)
    {
        var exists = await client.Indices.ExistsAsync(_options.ProductIndexName, cancellationToken);

        if (!exists.IsValidResponse)
        {
            throw new InvalidOperationException($"Elasticsearch недоступен: {exists.DebugInformation}");
        }

        if (exists.Exists)
        {
            return;
        }

        var created = await client.Indices.CreateAsync(
            ProductIndexMapping.Create(_options.ProductIndexName, _options.NumberOfShards, _options.NumberOfReplicas),
            cancellationToken);

        if (!created.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Не удалось создать индекс '{_options.ProductIndexName}': {created.DebugInformation}");
        }
    }

    public async Task<ProductIndexPage> SearchAsync(
        ProductSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        var request = new SearchRequest(_options.ProductIndexName)
        {
            From = (filter.NormalizedPage - 1) * filter.NormalizedPageSize,
            Size = filter.NormalizedPageSize,
            // Точное общее число совпадений нужно витрине для пагинации (по умолчанию ES отдаёт 10 000+).
            TrackTotalHits = new TrackHits(true),
            Query = BuildQuery(filter),
            Sort = BuildSort(filter.Sort),
        };

        var response = await client.SearchAsync<ProductDocument>(request, cancellationToken);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException($"Поиск в Elasticsearch не удался: {response.DebugInformation}");
        }

        return new ProductIndexPage(response.Documents.ToList(), (int)response.Total);
    }

    public async Task UpsertAsync(ProductDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        // Идемпотентно: идентификатор документа — идентификатор товара, поэтому повторная доставка
        // события обновляет документ, а не создаёт дубль.
        var response = await client.IndexAsync(
            document,
            _options.ProductIndexName,
            document.Id.ToString(),
            cancellationToken);

        if (!response.IsValidResponse)
        {
            throw new InvalidOperationException(
                $"Не удалось проиндексировать товар {document.Id}: {response.DebugInformation}");
        }
    }

    public async Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var response = await client.DeleteAsync<ProductDocument>(
            productId.ToString(),
            request => request.Index(_options.ProductIndexName),
            cancellationToken);

        // Документа может уже не быть (повторное событие, реконсиляция) — это не ошибка.
        if (!response.IsValidResponse && response.Result != Result.NotFound)
        {
            throw new InvalidOperationException(
                $"Не удалось удалить товар {productId} из индекса: {response.DebugInformation}");
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await client.PingAsync(cancellationToken);

            return response.IsValidResponse;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Проверка живости не должна падать: недоступный кластер — это Unhealthy, а не исключение.
            return false;
        }
    }

    private static Query BuildQuery(ProductSearchFilter filter)
    {
        // В индексе живут только опубликованные товары, но признак всё равно проверяется:
        // запрос становится самодостаточным и не покажет снятый товар даже после ручной правки индекса.
        var filters = new List<Query>
        {
            new TermQuery(new Field(ProductIndexMapping.IsPublishedField)) { Value = true },
        };

        if (filter.CategoryId is { } categoryId)
        {
            filters.Add(new TermQuery(new Field(ProductIndexMapping.CategoryIdField))
            {
                Value = categoryId.ToString(),
            });
        }

        var priceRange = new NumberRangeQuery(new Field(ProductIndexMapping.PriceField))
        {
            Gte = filter.MinPrice.HasValue ? (double)filter.MinPrice.Value : null,
            Lte = filter.MaxPrice.HasValue ? (double)filter.MaxPrice.Value : null,
        };

        if (priceRange.Gte is not null || priceRange.Lte is not null)
        {
            filters.Add(priceRange);
        }

        var query = new BoolQuery { Filter = filters };

        if (filter.NormalizedQuery is { } text)
        {
            // multi_match по названию (вес 3), описанию (2), марке растворителя и slug (2):
            // словом «установка» находятся все модели, а «TD60» — конкретная.
            query.Must = new List<Query>
            {
                new MultiMatchQuery
                {
                    Query = text,
                    Fields = ProductIndexMapping.SearchFields,
                    Type = TextQueryType.BestFields,
                },
            };
        }

        return query;
    }

    private static ICollection<SortOptions> BuildSort(ProductSort sort) => sort switch
    {
        ProductSort.PriceAsc => [FieldSort(ProductIndexMapping.PriceField, SortOrder.Asc)],
        ProductSort.PriceDesc => [FieldSort(ProductIndexMapping.PriceField, SortOrder.Desc)],
        ProductSort.NameAsc => [FieldSort(ProductIndexMapping.NameSortField, SortOrder.Asc)],
        ProductSort.NameDesc => [FieldSort(ProductIndexMapping.NameSortField, SortOrder.Desc)],
        // Релевантность: скор документа плюс стабильный tie-break по названию — без него
        // при равном скоре страницы выдачи могли бы «перемешиваться» между запросами.
        _ =>
        [
            SortOptions.Score(new ScoreSort { Order = SortOrder.Desc }),
            FieldSort(ProductIndexMapping.NameSortField, SortOrder.Asc),
        ],
    };

    private static SortOptions FieldSort(string field, SortOrder order)
        => SortOptions.Field(new Field(field), new FieldSort { Order = order });
}


using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.Mapping;

namespace Techodist.Search.Infrastructure.Elasticsearch;

/// <summary>
/// Маппинг индекса товаров (ADR 0009). Поля заданы явно, а не динамическим маппингом:
/// поиск по названию и описанию — полнотекстовый (анализатор <c>russian</c> даёт словоформы
/// «установка/установки»), а фильтры и сортировка работают по <c>keyword</c>/числовым полям,
/// которые не должны анализироваться. Поле <c>name.keyword</c> — стабильный tie-break пагинации.
/// </summary>
internal static class ProductIndexMapping
{
    /// <summary>Русский анализатор из стандартной поставки Elasticsearch (морфология + стоп-слова).</summary>
    internal const string TextAnalyzer = "russian";

    internal const string IdField = "id";
    internal const string NameField = "name";
    internal const string NameSortField = "name.keyword";
    internal const string ShortDescriptionField = "shortDescription";
    internal const string SlugField = "slug";
    internal const string SolventTypeField = "solventType";
    internal const string VolumeLitersField = "volumeLiters";
    internal const string PriceField = "price";
    internal const string CurrencyField = "currency";
    internal const string MainImageUrlField = "mainImageUrl";
    internal const string CategoryIdField = "categoryId";
    internal const string CategoryNameField = "categoryName";
    internal const string IsPublishedField = "isPublished";
    internal const string UpdatedAtField = "updatedAt";

    /// <summary>Поля, по которым ищет multi_match; вес названия выше описания.</summary>
    internal static readonly string[] SearchFields =
    [
        $"{NameField}^3",
        $"{ShortDescriptionField}^2",
        $"{CategoryNameField}",
        $"{SlugField}^2",
        $"{SolventTypeField}",
    ];

    public static CreateIndexRequest Create(string indexName, int numberOfShards, int numberOfReplicas) =>
        new(indexName)
        {
            Settings = new IndexSettings
            {
                NumberOfShards = numberOfShards,
                NumberOfReplicas = numberOfReplicas,
            },
            Mappings = new TypeMapping
            {
                Properties = new Properties
                {
                    { IdField, new KeywordProperty() },
                    { NameField, TextWithKeywordSubfield() },
                    { ShortDescriptionField, new TextProperty { Analyzer = TextAnalyzer } },
                    { SlugField, new KeywordProperty() },
                    { SolventTypeField, new KeywordProperty() },
                    { VolumeLitersField, new IntegerNumberProperty() },
                    { PriceField, new DoubleNumberProperty() },
                    { CurrencyField, new KeywordProperty() },
                    { MainImageUrlField, new KeywordProperty { Index = false } },
                    { CategoryIdField, new KeywordProperty() },
                    { CategoryNameField, TextWithKeywordSubfield() },
                    { IsPublishedField, new BooleanProperty() },
                    { UpdatedAtField, new DateProperty() },
                },
            },
        };

    /// <summary>Полнотекстовое поле с keyword-подполем: по нему сортируется выдача (<c>name.keyword</c>).</summary>
    private static TextProperty TextWithKeywordSubfield() => new()
    {
        Analyzer = TextAnalyzer,
        Fields = new Properties
        {
            { "keyword", new KeywordProperty() },
        },
    };
}

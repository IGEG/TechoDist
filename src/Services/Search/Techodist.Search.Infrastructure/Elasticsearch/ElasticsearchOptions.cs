namespace Techodist.Search.Infrastructure.Elasticsearch;

/// <summary>Настройки подключения к Elasticsearch (секция конфигурации "Elasticsearch").</summary>
public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";

    /// <summary>Адрес кластера (в docker-compose — http://localhost:9200).</summary>
    public string Uri { get; set; } = "http://localhost:9200";

    /// <summary>Имя индекса товаров.</summary>
    public string ProductIndexName { get; set; } = "techodist-products";

    /// <summary>Пользователь (Elasticsearch без X-Pack Security — пусто).</summary>
    public string? Username { get; set; }

    public string? Password { get; set; }

    /// <summary>Таймаут запроса к кластеру, секунд.</summary>
    public int RequestTimeoutSeconds { get; set; } = 10;

    /// <summary>Число шардов индекса; для учебного контура достаточно одного.</summary>
    public int NumberOfShards { get; set; } = 1;

    /// <summary>Число реплик: на одном узле реплики не размещаются, поэтому по умолчанию 0.</summary>
    public int NumberOfReplicas { get; set; }
}

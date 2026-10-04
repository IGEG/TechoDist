using System.Text.Json;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Techodist.Search.Application.Abstractions;
using Techodist.Search.Infrastructure.Catalog;
using Techodist.Search.Infrastructure.Elasticsearch;
using Techodist.Search.Infrastructure.HealthChecks;
using Techodist.Search.Infrastructure.Reconciliation;

namespace Techodist.Search.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Адрес Catalog API по умолчанию (dev-порт из launchSettings).</summary>
    private const string DefaultCatalogBaseUrl = "http://localhost:5101";

    /// <summary>Таймаут чтения каталога при реконсиляции: страница из 100 товаров читается быстро.</summary>
    private static readonly TimeSpan CatalogRequestTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Регистрация инфраструктуры поиска: клиент Elasticsearch, индекс товаров, проверка
    /// доступности кластера, чтение каталога для реконсиляции и фоновая сверка индекса.
    /// </summary>
    public static IServiceCollection AddSearchInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ElasticsearchOptions>(configuration.GetSection(ElasticsearchOptions.SectionName));
        services.Configure<IndexReconciliationOptions>(configuration.GetSection(IndexReconciliationOptions.SectionName));

        // Клиент Elasticsearch — singleton: у него собственный пул HTTP-соединений к кластеру.
        services.AddSingleton(provider =>
        {
            var settings = provider.GetRequiredService<IOptions<ElasticsearchOptions>>().Value;

            var clientSettings = new ElasticsearchClientSettings(new Uri(settings.Uri, UriKind.Absolute))
                // Имена полей документа — camelCase: тот же вид у маппинга индекса и у запросов,
                // иначе документы легли бы в поля с другими именами и поиск ничего бы не находил.
                .DefaultFieldNameInferrer(JsonNamingPolicy.CamelCase.ConvertName)
                .RequestTimeout(TimeSpan.FromSeconds(settings.RequestTimeoutSeconds));

            if (!string.IsNullOrWhiteSpace(settings.Username))
            {
                clientSettings = clientSettings.Authentication(
                    new BasicAuthentication(settings.Username, settings.Password ?? string.Empty));
            }

            return new ElasticsearchClient(clientSettings);
        });

        services.AddSingleton<IProductIndex, ElasticProductIndex>();

        var catalogBaseUrl = configuration["Services:Catalog:BaseUrl"] ?? DefaultCatalogBaseUrl;

        services.AddHttpClient<ICatalogProductSource, CatalogProductSource>(client =>
        {
            client.BaseAddress = new Uri(catalogBaseUrl, UriKind.Absolute);
            client.Timeout = CatalogRequestTimeout;
        });

        services.AddHealthChecks()
            .AddCheck<ElasticsearchHealthCheck>("search-elasticsearch");

        services.AddHostedService<IndexReconciliationService>();

        return services;
    }
}

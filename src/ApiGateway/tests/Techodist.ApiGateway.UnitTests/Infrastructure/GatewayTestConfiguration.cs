using Techodist.ApiGateway;
using Techodist.ApiGateway.Configuration;
using Microsoft.Extensions.Configuration;

namespace Techodist.ApiGateway.UnitTests.Infrastructure;

/// <summary>
/// Доступ к настоящей конфигурации шлюза (<c>appsettings.json</c> копируется в выход тестов).
/// Тесты проверяют реальные маршруты, поэтому опечатка в appsettings ломает сборку тестов,
/// а не прод-день.
/// </summary>
internal static class GatewayTestConfiguration
{
    public static IConfigurationRoot Build() => new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile(Path.Combine("gateway", "appsettings.json"), optional: false)
        .AddJsonFile(Path.Combine("gateway", "appsettings.Development.json"), optional: true)
        .Build();

    public static IReadOnlyDictionary<string, RouteConfig> GetRoutes(IConfiguration configuration) =>
        configuration.GetSection($"{GatewayConstants.ReverseProxySectionName}:Routes")
            .GetChildren()
            .ToDictionary(
                route => route.Key,
                route => new RouteConfig(
                    route["ClusterId"] ?? string.Empty,
                    route.GetValue<int>("Order"),
                    route["Match:Path"] ?? string.Empty,
                    route.GetSection("Match:Methods").Get<string[]>() ?? [],
                    route["AuthorizationPolicy"],
                    route["RateLimiterPolicy"],
                    route.GetSection("Transforms").GetChildren()
                        .Select(transform => (IReadOnlyDictionary<string, string>)transform.GetChildren()
                            .ToDictionary(property => property.Key, property => property.Value ?? string.Empty))
                        .ToList()));

    public static IReadOnlyDictionary<string, string> GetClusterDestinations(IConfiguration configuration) =>
        configuration.GetSection($"{GatewayConstants.ReverseProxySectionName}:Clusters")
            .GetChildren()
            .ToDictionary(
                cluster => cluster.Key,
                cluster => cluster.GetSection("Destinations:default:Address").Value ?? string.Empty);

    public static IReadOnlyList<GatewayServiceEndpoint> GetHealthServices(IConfiguration configuration) =>
        configuration.GetSection(GatewayConstants.ServicesSectionName).Get<GatewayServiceEndpoint[]>() ?? [];
}

/// <summary>Маршрут YARP в терминах, которые проверяют тесты.</summary>
internal sealed record RouteConfig(
    string ClusterId,
    int Order,
    string MatchPath,
    IReadOnlyList<string> Methods,
    string? AuthorizationPolicy,
    string? RateLimiterPolicy,
    IReadOnlyList<IReadOnlyDictionary<string, string>> Transforms)
{
    /// <summary>Префикс сервиса, вычисленный из пути маршрута (например <c>/catalog</c>).</summary>
    public string PathPrefix => MatchPath.Length > 1 && MatchPath[1..].Contains('/')
        ? MatchPath[..MatchPath.IndexOf('/', 1)]
        : MatchPath;

    /// <summary>Значения трансформа <c>PathRemovePrefix</c> (ожидается ровно один).</summary>
    public IReadOnlyList<string> RemovedPathPrefixes => Transforms
        .Select(transform => transform.GetValueOrDefault("PathRemovePrefix"))
        .Where(value => value is not null)
        .Select(value => value!)
        .ToList();
}

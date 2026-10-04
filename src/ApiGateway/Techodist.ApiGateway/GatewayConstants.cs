namespace Techodist.ApiGateway;

/// <summary>Ключи конфигурации и имена секций шлюза.</summary>
public static class GatewayConstants
{
    /// <summary>Секция со стандартным описанием маршрутов/кластеров YARP.</summary>
    public const string ReverseProxySectionName = "ReverseProxy";

    /// <summary>Секция с адресами сервисов для агрегированного health-check.</summary>
    public const string ServicesSectionName = "Gateway:Services";

    /// <summary>Тег регистрации health-check, попадающей в <c>/health/ready</c>.</summary>
    public const string ReadyTag = "ready";

    /// <summary>Префикс маршрута Catalog API (снимается трансформом перед проксированием).</summary>
    public const string CatalogPathPrefix = "/catalog";

    /// <summary>Префикс маршрута Identity API (в том числе token-endpoint <c>/identity/connect/token</c>).</summary>
    public const string IdentityPathPrefix = "/identity";

    /// <summary>Префикс маршрута Basket API (гостевая корзина, ADR 0005).</summary>
    public const string BasketPathPrefix = "/basket";

    /// <summary>
    /// Префикс маршрута Order API: гостевое оформление заявки (анонимно) и админские
    /// операции со статусами (роль <c>Admin</c>/<c>Manager</c>).
    /// </summary>
    public const string OrderPathPrefix = "/order";

    /// <summary>Префикс маршрута Search API (публичный поиск по товарам, ADR 0009).</summary>
    public const string SearchPathPrefix = "/search";

    public const string LivePath = "/health/live";

    public const string ReadyPath = "/health/ready";
}

using Techodist.ApiGateway;
using Techodist.ApiGateway.UnitTests.Infrastructure;
using Xunit;

namespace Techodist.ApiGateway.UnitTests.Gateway;

/// <summary>Контракт маршрутизации шлюза: префиксы сервисов, кластеры и трансформы.</summary>
public sealed class GatewayRouteConfigurationTests
{
    [Fact]
    public void EveryRoute_ReferencesAnExistingCluster()
    {
        var configuration = GatewayTestConfiguration.Build();
        var clusters = GatewayTestConfiguration.GetClusterDestinations(configuration);

        foreach (var (name, route) in GatewayTestConfiguration.GetRoutes(configuration))
        {
            Assert.True(
                clusters.ContainsKey(route.ClusterId),
                $"Маршрут '{name}' ссылается на несуществующий кластер '{route.ClusterId}'.");
        }
    }

    [Fact]
    public void EveryRoutePath_IsNamespacedByServicePrefix()
    {
        var configuration = GatewayTestConfiguration.Build();

        string[] prefixes =
        [
            GatewayConstants.CatalogPathPrefix,
            GatewayConstants.IdentityPathPrefix,
            GatewayConstants.BasketPathPrefix,
            GatewayConstants.OrderPathPrefix,
            GatewayConstants.SearchPathPrefix,
        ];

        foreach (var (name, route) in GatewayTestConfiguration.GetRoutes(configuration))
        {
            Assert.True(
                prefixes.Any(prefix => route.PathPrefix == prefix),
                $"Маршрут '{name}' ({route.MatchPath}) должен начинаться с префикса сервиса: " +
                "фронтенд знает только один адрес — gateway.");
        }
    }

    [Fact]
    public void EveryRoute_StripsItsServicePrefix()
    {
        var configuration = GatewayTestConfiguration.Build();

        foreach (var (name, route) in GatewayTestConfiguration.GetRoutes(configuration))
        {
            Assert.Equal([route.PathPrefix], route.RemovedPathPrefixes);
        }
    }

    [Fact]
    public void CatalogReads_ArePublicButCatalogWrites_RequireAdminRole()
    {
        var configuration = GatewayTestConfiguration.Build();
        var routes = GatewayTestConfiguration.GetRoutes(configuration);

        var catalog = routes["catalog-api"];
        Assert.Null(catalog.AuthorizationPolicy);
        Assert.Equal($"{GatewayConstants.CatalogPathPrefix}/{{**catch-all}}", catalog.MatchPath);

        // Все изменяющие операции каталога закрыты ролью: создание, правка, публикация, архив, удаление.
        var adminWrites = new Dictionary<string, string[]>
        {
            ["catalog-admin-products"] = ["POST"],
            ["catalog-admin-categories"] = ["POST"],
            ["catalog-admin-products-item"] = ["PUT", "DELETE"],
            ["catalog-admin-products-publish"] = ["POST"],
            ["catalog-admin-products-archive"] = ["POST"],
        };

        foreach (var (name, methods) in adminWrites)
        {
            var route = routes[name];

            Assert.Equal(GatewayPolicies.Admin, route.AuthorizationPolicy);
            Assert.Equal(methods, route.Methods);

            // Административный маршрут обязан быть точнее публичного catch-all, иначе его политика не выберется.
            Assert.True(route.Order < catalog.Order, $"У маршрута '{name}' должен быть меньший Order, чем у '{catalog}'.");
        }

        // Список товаров для админки отдаёт черновики и архив: витрине он недоступен.
        var adminList = routes["catalog-admin-products-list"];

        Assert.Equal(GatewayPolicies.Admin, adminList.AuthorizationPolicy);
        Assert.Equal(["GET"], adminList.Methods);
        Assert.Equal($"{GatewayConstants.CatalogPathPrefix}/api/products/admin", adminList.MatchPath);
        Assert.True(adminList.Order < catalog.Order, "Список админки должен иметь меньший Order, чем публичный catch-all.");

        // Карточка товара по-прежнему читается гостями: закрыты только PUT/DELETE.
        Assert.DoesNotContain("GET", routes["catalog-admin-products-item"].Methods);
    }

    [Fact]
    public void BasketRoute_IsAnonymousBecauseGuestHasNoToken()
    {
        var configuration = GatewayTestConfiguration.Build();

        var basket = GatewayTestConfiguration.GetRoutes(configuration)["basket-api"];

        Assert.Equal("basket", basket.ClusterId);
        Assert.Equal($"{GatewayConstants.BasketPathPrefix}/{{**catch-all}}", basket.MatchPath);

        // Гостевая корзина (ADR 0005): до оформления заявки токена нет, значит маршрут не должен требовать JWT.
        Assert.Null(basket.AuthorizationPolicy);
    }

    [Fact]
    public void SearchRoute_IsPublicBecauseSearchIsPartOfTheStorefront()
    {
        var configuration = GatewayTestConfiguration.Build();

        var search = GatewayTestConfiguration.GetRoutes(configuration)["search-api"];

        Assert.Equal("search", search.ClusterId);
        Assert.Equal($"{GatewayConstants.SearchPathPrefix}/{{**catch-all}}", search.MatchPath);

        // Поиск — публичная витрина: покупатель ищет товар без входа в систему (ADR 0004, 0009).
        Assert.Null(search.AuthorizationPolicy);
    }

    [Fact]
    public void OrderRoutes_SplitGuestAndAdminSurface()
    {
        var configuration = GatewayTestConfiguration.Build();
        var routes = GatewayTestConfiguration.GetRoutes(configuration);

        var submit = routes["order-guest-submit"];

        Assert.Equal("order", submit.ClusterId);
        Assert.Equal($"{GatewayConstants.OrderPathPrefix}/api/orders", submit.MatchPath);
        Assert.Equal(["POST"], submit.Methods);

        // Гость не аутентифицируется (ADR 0005), но заявка — единственный анонимный POST
        // бизнес-данных, поэтому маршрут ограничен отдельным жёстким лимитом.
        Assert.Null(submit.AuthorizationPolicy);
        Assert.Equal(GatewayPolicies.GuestOrderRateLimit, submit.RateLimiterPolicy);

        var status = routes["order-guest-status"];

        Assert.Null(status.AuthorizationPolicy);
        Assert.Equal($"{GatewayConstants.OrderPathPrefix}/api/orders/number/{{**catch-all}}", status.MatchPath);

        var admin = routes["order-admin"];

        Assert.Equal(GatewayPolicies.Admin, admin.AuthorizationPolicy);
        Assert.Equal($"{GatewayConstants.OrderPathPrefix}/api/orders/{{**catch-all}}", admin.MatchPath);

        // Гостевые маршруты обязаны быть точнее админского catch-all, иначе гость получит 401.
        Assert.True(submit.Order < admin.Order, "Оформление заявки должно иметь меньший Order, чем админский catch-all.");
        Assert.True(status.Order < admin.Order, "Проверка статуса должна иметь меньший Order, чем админский catch-all.");
    }

    [Fact]
    public void TokenEndpoint_IsAnonymousButRateLimited()
    {
        var configuration = GatewayTestConfiguration.Build();

        var token = GatewayTestConfiguration.GetRoutes(configuration)["identity-token"];

        Assert.Equal($"{GatewayConstants.IdentityPathPrefix}/connect/token", token.MatchPath);
        Assert.Null(token.AuthorizationPolicy);
        Assert.Equal(GatewayPolicies.AuthenticationRateLimit, token.RateLimiterPolicy);
    }

    [Fact]
    public void IdentityUserManagement_IsAdminOnly()
    {
        var configuration = GatewayTestConfiguration.Build();

        var adminUsers = GatewayTestConfiguration.GetRoutes(configuration)["identity-admin-users"];

        Assert.Equal(GatewayPolicies.AdminOnly, adminUsers.AuthorizationPolicy);
        Assert.Equal($"{GatewayConstants.IdentityPathPrefix}/api/admin/{{**catch-all}}", adminUsers.MatchPath);
    }

    [Fact]
    public void RoutedPolicies_AreDeclaredInCode()
    {
        var configuration = GatewayTestConfiguration.Build();

        string[] authorizationPolicies = [GatewayPolicies.Admin, GatewayPolicies.AdminOnly];
        string[] rateLimiterPolicies =
        [
            GatewayPolicies.GeneralRateLimit,
            GatewayPolicies.AuthenticationRateLimit,
            GatewayPolicies.GuestOrderRateLimit,
        ];

        foreach (var (name, route) in GatewayTestConfiguration.GetRoutes(configuration))
        {
            if (route.AuthorizationPolicy is not null)
            {
                Assert.True(
                    authorizationPolicies.Contains(route.AuthorizationPolicy),
                    $"Маршрут '{name}' ссылается на неизвестную политику авторизации '{route.AuthorizationPolicy}'.");
            }

            if (route.RateLimiterPolicy is not null)
            {
                Assert.True(
                    rateLimiterPolicies.Contains(route.RateLimiterPolicy),
                    $"Маршрут '{name}' ссылается на неизвестную политику rate-limit '{route.RateLimiterPolicy}'.");
            }
        }
    }
}

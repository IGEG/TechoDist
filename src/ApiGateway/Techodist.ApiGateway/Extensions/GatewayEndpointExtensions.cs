using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace Techodist.ApiGateway.Extensions;

/// <summary>Диагностический эндпоинт-«визитка» шлюза.</summary>
public static class GatewayEndpointExtensions
{
    public static WebApplication MapGatewayIndex(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Не раскрывает внутренние адреса сервисов, но подсказывает фронтенду,
        // какие префиксы проксируются (см. docs/adr/0007-yarp-api-gateway.md).
        app.MapGet("/", () => Results.Ok(new
        {
            service = "Techodist API Gateway",
            prefixes = new[]
            {
                $"{GatewayConstants.CatalogPathPrefix}/api/…",
                $"{GatewayConstants.BasketPathPrefix}/api/… (гостевая корзина, cookie с basketId)",
                $"{GatewayConstants.OrderPathPrefix}/api/orders (оформление заявки гостем из своей корзины)",
                $"{GatewayConstants.OrderPathPrefix}/api/orders/number/… (статус заявки по номеру, без токена)",
                $"{GatewayConstants.OrderPathPrefix}/api/orders/… (список/статусы для админ-панели)",
                $"{GatewayConstants.IdentityPathPrefix}/api/… (роль Admin или Manager)",
                $"{GatewayConstants.IdentityPathPrefix}/connect/token (выдача access/refresh-токенов)",
                // Публичный поиск по индексу Elasticsearch: токен не нужен, как у витрины каталога (ADR 0009).
                $"{GatewayConstants.SearchPathPrefix}/api/search/products (поиск товаров)",
            },
            health = new[] { GatewayConstants.LivePath, GatewayConstants.ReadyPath },
        })).DisableRateLimiting();

        return app;
    }
}

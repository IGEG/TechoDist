using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Techodist.ApiGateway.Extensions;

/// <summary>
/// Rate-limiting шлюза (ADR 0007: «rate-limiting публичных форм»). Разделяем два режима:
/// общий лимит на пользователя/IP и отдельный жёсткий лимит на выдачу токена.
/// </summary>
public static class RateLimitingExtensions
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    /// <summary>Общий лимит: ~10 запросов в секунду на клиента (сглаживает всплески и парсеров).</summary>
    private const int GeneralPermitLimit = 600;

    /// <summary>Лимит token-endpoint: 10 попыток входа в минуту — защита от перебора пароля.</summary>
    private const int AuthenticationPermitLimit = 10;

    /// <summary>Лимит оформления заявки: 5 отправок за окно — форма контактов открыта анонимно.</summary>
    private const int GuestOrderPermitLimit = 5;

    /// <summary>Окно лимита оформления заявки: подобрать столько же заявок в минуту спамер не должен.</summary>
    private static readonly TimeSpan GuestOrderWindow = TimeSpan.FromMinutes(10);

    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Глобальный лимитер применяется ко всем маршрутам, у которых не задан
            // собственный RateLimiterPolicy (health-эндпоинты его отключают).
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    ResolvePartitionKey(context),
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = GeneralPermitLimit,
                        Window = Window,
                        SegmentsPerWindow = 6,
                        QueueLimit = 0,
                    }));

            options.AddPolicy(GatewayPolicies.AuthenticationRateLimit, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ResolvePartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = AuthenticationPermitLimit,
                        Window = Window,
                        QueueLimit = 0,
                    }));

            // Оформление заявки — единственный анонимный POST бизнес-данных (гость без токена),
            // поэтому лимит жёстче общего: партиционирование по IP (пользователя ещё нет).
            options.AddPolicy(GatewayPolicies.GuestOrderRateLimit, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    ResolvePartitionKey(context),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = GuestOrderPermitLimit,
                        Window = GuestOrderWindow,
                        QueueLimit = 0,
                    }));

            options.OnRejected = OnRejectedAsync;
        });

        return services;
    }

    /// <summary>
    /// Ключ партиционирования: имя пользователя, если он аутентифицирован, иначе IP.
    /// IP берётся с учётом <c>X-Forwarded-For</c> (см. <c>UseForwardedHeaders</c>).
    /// </summary>
    private static string ResolvePartitionKey(HttpContext context)
    {
        var userName = context.User.Identity?.Name;

        return string.IsNullOrWhiteSpace(userName)
            ? context.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            : userName;
    }

    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Слишком много запросов",
            Detail = "Превышен лимит запросов к API. Повторите попытку позже.",
            Instance = httpContext.Request.Path,
        };
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
    }
}

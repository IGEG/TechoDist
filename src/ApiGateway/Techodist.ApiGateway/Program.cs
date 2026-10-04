using Techodist.ApiGateway;
using Techodist.ApiGateway.Extensions;
using Techodist.BuildingBlocks.Observability;
using Techodist.BuildingBlocks.Web.Extensions;
using Techodist.BuildingBlocks.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddTechodistObservability("api-gateway");

builder.Services.AddGatewayForwardedHeaders();
builder.Services.AddTechodistCors(builder.Configuration);

// Шлюз проверяет подпись, issuer и срок жизни access-токена, но не аудиторию:
// у одного клиента (админ-панели) токены с разными аудиториями (Catalog/Identity/Order),
// а строгую проверку aud выполняет сервис-получатель (defense in depth).
builder.Services.AddTechodistJwtAuthentication(builder.Configuration);
builder.Services.AddGatewayAuthorizationPolicies();
builder.Services.AddGatewayRateLimiting();
builder.Services.AddGatewayHealthChecks(builder.Configuration);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection(GatewayConstants.ReverseProxySectionName));

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

// Логирование запросов и эндпоинт метрик: раньше middleware, обслуживающих запросы,
// чтобы в лог попали и отказы (401/429/503), и время работы всего пайплайна шлюза.
app.UseTechodistObservability();

// CORS до маршрутизации: preflight (OPTIONS) обслуживает middleware и до YARP не доходит.
app.UseCors(WebExtensions.CorsPolicyName);

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Лимитер после маршрутизации: политики маршрутов YARP (RateLimiterPolicy) попадают
// в метаданные эндпоинта, а остальные запросы ограничивает глобальный лимитер.
app.UseRateLimiter();

app.MapGatewayHealthEndpoints();
app.MapGatewayIndex();
app.MapReverseProxy();

app.Run();

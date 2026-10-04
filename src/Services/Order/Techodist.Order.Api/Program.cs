using Techodist.BuildingBlocks.Observability;
using Techodist.BuildingBlocks.Web.Extensions;
using Techodist.Order.Api;
using Techodist.Order.Api.Orders;
using Techodist.Order.Application;
using Techodist.Order.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddTechodistObservability("order-api");

// Заявка оформляется из корзины гостя: basketId лежит в том же анонимном cookie,
// что и на витрине (ADR 0005), поэтому нужен доступ к текущему HttpContext.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BasketIdReader>();

builder.Services.AddOrderApplication();
builder.Services.AddOrderInfrastructure(builder.Configuration);
builder.Services.AddTechodistWebApi(builder.Configuration, "Techodist · Order API");

// Админ-эндпоинты (список заявок, смена статуса) проверяют токен админ-панели;
// аудитория — собственное имя API, как в Catalog.
builder.Services.AddTechodistJwtAuthentication(builder.Configuration, "techodist-order-api");

var app = builder.Build();

await OrderDbInitializer.InitializeAsync(app.Services, app.Logger);

app.UseTechodistObservability();
app.UseTechodistWebApi();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

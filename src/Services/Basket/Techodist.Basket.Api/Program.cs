using Techodist.Basket.Api.Baskets;
using Techodist.Basket.Application;
using Techodist.Basket.Infrastructure;
using Techodist.BuildingBlocks.Observability;
using Techodist.BuildingBlocks.Web.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddTechodistObservability("basket-api");

// basketId приходит из cookie, поэтому провайдеру нужен доступ к текущему HttpContext.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<BasketIdProvider>();

builder.Services.AddBasketApplication();
builder.Services.AddBasketInfrastructure(builder.Configuration);
builder.Services.AddTechodistWebApi(builder.Configuration, "Techodist · Basket API");

var app = builder.Build();

app.UseTechodistObservability();
app.UseTechodistWebApi();
app.MapControllers();

app.Run();

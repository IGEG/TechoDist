using Techodist.BuildingBlocks.Observability;
using Techodist.BuildingBlocks.Web.Extensions;
using Techodist.Catalog.Api;
using Techodist.Catalog.Application;
using Techodist.Catalog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddTechodistObservability("catalog-api");

builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddTechodistWebApi(builder.Configuration, "Techodist · Catalog API");

// Токены выпускает Identity API (OpenIddict), Catalog их только проверяет:
// аудитория — собственное имя API, то есть access-токен админ-панели с scope techodist-catalog-api.
builder.Services.AddTechodistJwtAuthentication(builder.Configuration, "techodist-catalog-api");

var app = builder.Build();

await CatalogDataSeeder.SeedAsync(app.Services, app.Logger);

app.UseTechodistWebApi();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

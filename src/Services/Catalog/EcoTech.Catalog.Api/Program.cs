using EcoTech.BuildingBlocks.Observability;
using EcoTech.BuildingBlocks.Web.Extensions;
using EcoTech.Catalog.Api;
using EcoTech.Catalog.Application;
using EcoTech.Catalog.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddEcoTechObservability("catalog-api");

builder.Services.AddCatalogApplication();
builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddEcoTechWebApi(builder.Configuration, "EcoTech · Catalog API");

var app = builder.Build();

await CatalogDataSeeder.SeedAsync(app.Services, app.Logger);

app.UseEcoTechWebApi();
app.MapControllers();

app.Run();

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

var app = builder.Build();

await CatalogDataSeeder.SeedAsync(app.Services, app.Logger);

app.UseTechodistWebApi();
app.MapControllers();

app.Run();

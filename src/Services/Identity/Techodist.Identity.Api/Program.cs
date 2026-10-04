using Techodist.BuildingBlocks.Observability;
using Techodist.BuildingBlocks.Web.Extensions;
using Techodist.Identity.Api;
using Techodist.Identity.Api.Security;
using Techodist.Identity.Application;
using Techodist.Identity.Application.Abstractions;
using Techodist.Identity.Infrastructure;
using Techodist.Identity.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.AddTechodistObservability("identity-api");

builder.Services.AddIdentityApplication();
builder.Services.AddIdentityInfrastructure(builder.Configuration, builder.Environment);

// Access-токены подписаны тем же симметричным ключом, что проверяет JwtBearer, а аудитория
// админ-панели — "techodist-identity-api": так защищаются собственные эндпоинты Identity API.
builder.Services.AddTechodistJwtAuthentication(builder.Configuration, OpenIddictDataSeeder.IdentityApiScope);
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.AddTechodistWebApi(builder.Configuration, "Techodist · Identity API");

var app = builder.Build();

await IdentityDataSeeder.SeedAsync(app.Services, app.Configuration, app.Logger);

app.UseTechodistObservability();
app.UseTechodistWebApi();

// UseAuthentication также включает обработку /connect/token сервером OpenIddict
// (token-endpoint обслуживается AuthorizationController).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

using Techodist.BuildingBlocks.Observability;
using Techodist.BuildingBlocks.Web.Extensions;
using Techodist.Search.Api.Extensions;
using Techodist.Search.Application;
using Techodist.Search.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddTechodistObservability("search-api");

builder.Services.AddSearchApplication();
builder.Services.AddSearchInfrastructure(builder.Configuration);

// Индекс наполняется событием ProductChanged из outbox каталога (ADR 0003, 0009), а фоновая
// сверка доводит его до каталога, если событие потерялось. Своего хранилища у сервиса нет,
// поэтому transactional outbox и БД ему не нужны — как Notification.
builder.Services.AddSearchConsumers(builder.Configuration);

builder.Services.AddTechodistWebApi(builder.Configuration, "Techodist · Search API");

// Поиск витрины публичный (ADR 0004): покупатели не аутентифицируются, админских операций
// у сервиса нет, поэтому JWT-аутентификация в Search не подключается.

var app = builder.Build();

app.UseTechodistWebApi();
app.MapControllers();

app.Run();

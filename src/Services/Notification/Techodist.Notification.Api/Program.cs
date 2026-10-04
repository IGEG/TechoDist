using Techodist.BuildingBlocks.Observability;
using Techodist.BuildingBlocks.Web.Extensions;
using Techodist.Notification.Api.Extensions;
using Techodist.Notification.Application;
using Techodist.Notification.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddTechodistObservability("notification-api");

builder.Services.AddNotificationApplication(builder.Configuration);
builder.Services.AddNotificationInfrastructure(builder.Configuration);

// События заявок приходят из outbox сервиса Order (ADR 0003); собственного хранилища
// у сервиса нет — только потребители, шаблоны писем и SMTP.
builder.Services.AddNotificationConsumers(builder.Configuration);

builder.Services.AddTechodistWebApi(builder.Configuration, "Techodist · Notification API");

var app = builder.Build();

app.UseTechodistObservability();
app.UseTechodistWebApi();

app.Run();

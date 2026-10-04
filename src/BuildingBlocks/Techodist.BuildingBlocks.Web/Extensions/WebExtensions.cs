using System.Text.Json.Serialization;
using Techodist.BuildingBlocks.Web.Health;
using Techodist.BuildingBlocks.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Techodist.BuildingBlocks.Web.Extensions;

/// <summary>
/// Типовое подключение Web API: контроллеры, Swagger, ProblemDetails,
/// глобальный обработчик исключений, CORS и health-checks.
/// </summary>
public static class WebExtensions
{
    public const string CorsPolicyName = "techodist";

    public static IServiceCollection AddTechodistWebApi(
        this IServiceCollection services,
        IConfiguration configuration,
        string apiTitle)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = apiTitle,
                Version = "v1",
                Description = "Techodist — интернет-магазин оборудования для регенерации растворителей",
            });
        });

        services.AddProblemDetails();
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddHealthChecks();

        services.AddTechodistCors(configuration);

        return services;
    }

    /// <summary>
    /// Единая политика CORS <see cref="CorsPolicyName"/> по списку <c>Cors:AllowedOrigins</c>.
    /// Вынесена отдельно: её использует и Web API, и API Gateway (без MVC-части).
    /// </summary>
    public static IServiceCollection AddTechodistCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins);
            }

            policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }));

        return services;
    }

    public static WebApplication UseTechodistWebApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseCors(CorsPolicyName);
        app.MapTechodistHealthEndpoints();

        return app;
    }
}

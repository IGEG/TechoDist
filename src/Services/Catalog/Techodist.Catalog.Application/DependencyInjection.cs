using System.Reflection;
using Techodist.Catalog.Application.Behaviours;
using Techodist.Catalog.Application.Common;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Techodist.Catalog.Application;

public static class DependencyInjection
{
    /// <summary>Регистрация слоя Application: MediatR, behaviors, валидаторы, маппинг.</summary>
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        CatalogMappingConfig.Register(TypeAdapterConfig.GlobalSettings);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehaviour<,>));
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}

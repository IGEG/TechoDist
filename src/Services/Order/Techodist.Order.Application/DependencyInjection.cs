using System.Reflection;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Techodist.BuildingBlocks.Application.Behaviours;
using Techodist.Order.Application.Common;

namespace Techodist.Order.Application;

public static class DependencyInjection
{
    /// <summary>Регистрация слоя Application: MediatR, behaviors, валидаторы, маппинг.</summary>
    public static IServiceCollection AddOrderApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        OrderMappingConfig.Register(TypeAdapterConfig.GlobalSettings);

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

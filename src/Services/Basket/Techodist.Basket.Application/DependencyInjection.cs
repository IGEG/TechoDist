using System.Reflection;
using Techodist.Basket.Application.Common;
using Techodist.BuildingBlocks.Application.Behaviours;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Techodist.Basket.Application;

public static class DependencyInjection
{
    /// <summary>Регистрация слоя Application: MediatR, behaviors, валидаторы, маппинг.</summary>
    public static IServiceCollection AddBasketApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        BasketMappingConfig.Register(TypeAdapterConfig.GlobalSettings);

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

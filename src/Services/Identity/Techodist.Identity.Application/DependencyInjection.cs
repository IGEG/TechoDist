using System.Reflection;
using Techodist.BuildingBlocks.Application.Behaviours;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Techodist.Identity.Application;

public static class DependencyInjection
{
    /// <summary>Регистрация слоя Application: MediatR, behaviors (из BuildingBlocks), валидаторы FluentValidation.</summary>
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

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

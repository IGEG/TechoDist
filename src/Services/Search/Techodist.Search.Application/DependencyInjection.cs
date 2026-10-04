using System.Reflection;
using MediatR;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Techodist.BuildingBlocks.Application.Behaviours;
using Techodist.Search.Application.Common;
using Techodist.Search.Application.Features.ProductChanged;
using Techodist.Search.Application.Features.Reindex;

namespace Techodist.Search.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Регистрация слоя Application: MediatR (поисковый запрос витрины), правила маппинга и
    /// обработчики, которые вызывает транспортный слой (событие каталога и реконсиляция).
    /// Валидация входных параметров живёт в <c>ProductSearchFilter</c>: это нормализация
    /// диапазонов и страниц, отдельные валидаторы FluentValidation поиску ничего не добавляют.
    /// </summary>
    public static IServiceCollection AddSearchApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        SearchMappingConfig.Register(TypeAdapterConfig.GlobalSettings);

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehaviour<,>));
        });

        services.AddScoped<ProductChangedIndexer>();
        services.AddScoped<CatalogIndexReconciler>();

        return services;
    }
}

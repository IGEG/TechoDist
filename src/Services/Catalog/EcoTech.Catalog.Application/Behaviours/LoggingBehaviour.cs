using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EcoTech.Catalog.Application.Behaviours;

/// <summary>
/// MediatR pipeline: структурное логирование и замер времени обработки запроса.
/// </summary>
public sealed class LoggingBehaviour<TRequest, TResponse>(ILogger<LoggingBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int SlowRequestThresholdMs = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Обработка запроса {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            logger.LogWarning(
                "Запрос {RequestName} обрабатывался долго: {ElapsedMs} мс",
                requestName,
                stopwatch.ElapsedMilliseconds);
        }
        else
        {
            logger.LogInformation(
                "Запрос {RequestName} обработан за {ElapsedMs} мс",
                requestName,
                stopwatch.ElapsedMilliseconds);
        }

        return response;
    }
}

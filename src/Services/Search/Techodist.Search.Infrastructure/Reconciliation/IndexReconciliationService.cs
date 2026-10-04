using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Techodist.Search.Application.Features.Reindex;

namespace Techodist.Search.Infrastructure.Reconciliation;

/// <summary>
/// Периодическая сверка индекса с каталогом (ADR 0009): сервис поиска мог быть недоступен,
/// пока каталог менялся, поэтому индекс догоняет каталог при старте и далее по расписанию.
/// Сбой сверки не роняет сервис — индекс продолжает обновляться событиями, а следующий проход
/// доведёт состояние до каталога.
/// </summary>
internal sealed class IndexReconciliationService(
    IServiceScopeFactory scopeFactory,
    IOptions<IndexReconciliationOptions> options,
    ILogger<IndexReconciliationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var interval = TimeSpan.FromMinutes(settings.IntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            await ReconcileOnceAsync(stoppingToken);

            if (!settings.Enabled || interval <= TimeSpan.Zero)
            {
                logger.LogInformation("Периодическая сверка индекса отключена — выполнена только стартовая.");

                return;
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task ReconcileOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var reconciler = scope.ServiceProvider.GetRequiredService<CatalogIndexReconciler>();

            await reconciler.ReconcileAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Остановка сервиса — не ошибка сверки.
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Сверка поискового индекса с каталогом не удалась — повтор при следующем проходе.");
        }
    }
}

using Microsoft.Extensions.Logging;
using Techodist.Catalog.Application.Abstractions;

namespace Techodist.Catalog.Application.Common;

/// <summary>
/// Сбрасывает кэш списков каталога после изменения товара: в Redis пишется новая версия, а ключи
/// страниц содержат версию (<see cref="CatalogCacheKeys"/>) — прежние записи становятся
/// недостижимыми и истекают сами по TTL. Сбой Redis не должен ломать команду: каталог важнее кэша,
/// поэтому ошибка только логируется (устаревшие страницы живут не дольше TTL).
/// </summary>
internal sealed class CatalogCacheInvalidator(ICacheService cache, ILogger<CatalogCacheInvalidator> logger)
{
    public async Task InvalidateProductsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.SetAsync(
                CatalogCacheKeys.ProductListVersion,
                DateTimeOffset.UtcNow.Ticks,
                ttl: null,
                cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Не удалось обновить версию кэша каталога: витрина может отдавать устаревшие данные до истечения TTL.");
        }
    }
}

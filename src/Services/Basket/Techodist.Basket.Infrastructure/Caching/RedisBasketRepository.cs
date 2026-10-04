using System.Text.Json;
using Techodist.Basket.Application.Abstractions;
using Techodist.Basket.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Techodist.Basket.Infrastructure.Caching;

/// <summary>
/// Хранилище корзин в Redis через <see cref="IDistributedCache"/>: одна корзина — одно значение
/// (ключ <c>basket:{basketId}</c>) с TTL. Агрегат сохраняется целиком: корзина небольшого размера,
/// а атомарность «одна корзина = один ключ» убирает частично записанные состояния.
/// </summary>
internal sealed class RedisBasketRepository(
    IDistributedCache cache,
    IOptions<BasketStorageOptions> options,
    ILogger<RedisBasketRepository> logger)
    : IBasketRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<ShoppingBasket?> GetAsync(Guid basketId, CancellationToken cancellationToken = default)
    {
        var json = await cache.GetStringAsync(Key(basketId), cancellationToken);

        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            var state = JsonSerializer.Deserialize<BasketState>(json, SerializerOptions);

            return state is null ? null : BasketStateMapper.ToDomain(state);
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            // Повреждённое состояние (старая схема, ручная правка ключа) не должно ломать витрину:
            // считаем корзину отсутствующей, следующее добавление создаст её заново.
            logger.LogWarning(exception, "Состояние корзины {BasketId} повреждено и будет пересоздано.", basketId);

            return null;
        }
    }

    public Task SaveAsync(ShoppingBasket basket, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(basket);

        var json = JsonSerializer.Serialize(BasketStateMapper.FromDomain(basket), SerializerOptions);
        var entryOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = options.Value.Ttl };

        return cache.SetStringAsync(Key(basket.Id), json, entryOptions, cancellationToken);
    }

    public Task DeleteAsync(Guid basketId, CancellationToken cancellationToken = default)
        => cache.RemoveAsync(Key(basketId), cancellationToken);

    private string Key(Guid basketId) => $"{options.Value.KeyPrefix}{basketId}";
}

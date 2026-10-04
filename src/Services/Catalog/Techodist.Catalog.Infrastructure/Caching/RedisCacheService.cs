using System.Text.Json;
using Techodist.Catalog.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;

namespace Techodist.Catalog.Infrastructure.Caching;

/// <summary>Кэш на базе Redis (IDistributedCache) с JSON-сериализацией.</summary>
internal sealed class RedisCacheService(IDistributedCache cache) : ICacheService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var json = await cache.GetStringAsync(key, cancellationToken);

        return string.IsNullOrEmpty(json)
            ? default
            : JsonSerializer.Deserialize<T>(json, SerializerOptions);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value, SerializerOptions);

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl,
        };

        return cache.SetStringAsync(key, json, options, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => cache.RemoveAsync(key, cancellationToken);

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);

        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);

        if (value is not null)
        {
            await SetAsync(key, value, ttl, cancellationToken);
        }

        return value;
    }
}

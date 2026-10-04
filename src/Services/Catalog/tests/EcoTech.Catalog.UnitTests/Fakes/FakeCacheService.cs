using EcoTech.Catalog.Application.Abstractions;

namespace EcoTech.Catalog.UnitTests.Fakes;

/// <summary>Простой in-memory кэш для тестов.</summary>
internal sealed class FakeCacheService : ICacheService
{
    private readonly Dictionary<string, object> _store = [];

    public int SetCalls { get; private set; }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.TryGetValue(key, out var value) ? (T?)value : default);

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken cancellationToken = default)
    {
        SetCalls++;
        _store[key] = value!;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _store.Remove(key);
        return Task.CompletedTask;
    }

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
        await SetAsync(key, value, ttl, cancellationToken);

        return value;
    }
}

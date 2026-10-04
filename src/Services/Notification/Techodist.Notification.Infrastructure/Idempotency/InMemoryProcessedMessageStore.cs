using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Techodist.Notification.Application.Abstractions;

namespace Techodist.Notification.Infrastructure.Idempotency;

/// <summary>
/// Журнал обработанных сообщений в памяти процесса. Так делает dev-контур: у Notification
/// нет своей БД (docs/architecture.md, §«Сервисы»), а дедупликацию по одному узлу закрывает
/// ConcurrentDictionary. Для нескольких реплик журнал нужно вынести в общее хранилище
/// (inbox-таблица или Redis) — интерфейс <see cref="IProcessedMessageStore"/> для этого и нужен.
/// </summary>
public sealed class InMemoryProcessedMessageStore : IProcessedMessageStore
{
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _processed = new();

    private readonly ProcessedMessageStoreOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InMemoryProcessedMessageStore> _logger;

    public InMemoryProcessedMessageStore(
        IOptions<ProcessedMessageStoreOptions> options,
        TimeProvider timeProvider,
        ILogger<InMemoryProcessedMessageStore> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    private DateTimeOffset Now => _timeProvider.GetUtcNow();

    public Task<bool> HasProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_processed.TryGetValue(messageId, out var processedAt))
        {
            return Task.FromResult(false);
        }

        if (IsExpired(processedAt))
        {
            _processed.TryRemove(messageId, out _);

            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_options.Retention <= TimeSpan.Zero)
        {
            // Нулевой срок хранения — дедупликация выключена (осознанный dev-режим «каждое сообщение — письмо»).
            return Task.CompletedTask;
        }

        _processed[messageId] = Now;

        Evict();

        return Task.CompletedTask;
    }

    private bool IsExpired(DateTimeOffset processedAt) => _options.Retention > TimeSpan.Zero
        && Now - processedAt > _options.Retention;

    private void Evict()
    {
        var now = Now;

        foreach (var (messageId, processedAt) in _processed)
        {
            if (_options.Retention > TimeSpan.Zero && now - processedAt > _options.Retention)
            {
                _processed.TryRemove(messageId, out _);
            }
        }

        var overflow = _processed.Count - _options.Capacity;

        if (overflow <= 0)
        {
            return;
        }

        foreach (var (messageId, _) in _processed.OrderBy(pair => pair.Value).Take(overflow))
        {
            _processed.TryRemove(messageId, out _);
        }

        _logger.LogWarning(
            "Журнал обработанных сообщений переполнен: вытеснено {Overflow} старейших отметок (лимит {Capacity}).",
            overflow,
            _options.Capacity);
    }
}

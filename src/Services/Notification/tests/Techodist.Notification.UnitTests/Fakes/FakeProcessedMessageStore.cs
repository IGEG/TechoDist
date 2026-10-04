using Techodist.Notification.Application.Abstractions;

namespace Techodist.Notification.UnitTests.Fakes;

/// <summary>Подменный журнал обработанных сообщений: держит отметки в памяти набора.</summary>
internal sealed class FakeProcessedMessageStore : IProcessedMessageStore
{
    private readonly HashSet<Guid> _processed = [];

    public int MarkCalls { get; private set; }

    public IReadOnlyCollection<Guid> Processed => _processed;

    public Task<bool> HasProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
        => Task.FromResult(_processed.Contains(messageId));

    public Task MarkProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        MarkCalls++;
        _processed.Add(messageId);

        return Task.CompletedTask;
    }
}

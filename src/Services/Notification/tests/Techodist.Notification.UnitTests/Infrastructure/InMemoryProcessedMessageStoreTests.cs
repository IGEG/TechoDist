using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Techodist.Notification.Infrastructure.Idempotency;
using Techodist.Notification.UnitTests.Fakes;
using Xunit;

namespace Techodist.Notification.UnitTests.Infrastructure;

/// <summary>
/// Журнал обработанных сообщений в памяти: дедупликация повторов, срок хранения отметок
/// и вытеснение при переполнении. Время управляется через TimeProvider, поэтому тесты
/// не зависят от реальных часов.
/// </summary>
public sealed class InMemoryProcessedMessageStoreTests
{
    private static readonly DateTimeOffset Start = new(2026, 10, 4, 9, 0, 0, TimeSpan.Zero);

    private static readonly Guid FirstId = Guid.Parse("aaaaaaaa-1111-1111-1111-111111111111");

    private static readonly Guid SecondId = Guid.Parse("bbbbbbbb-2222-2222-2222-222222222222");

    private static readonly Guid ThirdId = Guid.Parse("cccccccc-3333-3333-3333-333333333333");

    [Fact]
    public async Task HasProcessed_UnknownMessage_IsNotDuplicate()
    {
        var store = Store();

        Assert.False(await store.HasProcessedAsync(FirstId, CancellationToken.None));
    }

    [Fact]
    public async Task MarkProcessed_MakesMessageDuplicate()
    {
        var store = Store();

        await store.MarkProcessedAsync(FirstId, CancellationToken.None);

        Assert.True(await store.HasProcessedAsync(FirstId, CancellationToken.None));
    }

    [Fact]
    public async Task Marked_Message_StaysDuplicateUntilRetentionEnds()
    {
        var time = new TestTimeProvider(Start);
        var store = Store(time, retention: TimeSpan.FromHours(1));

        await store.MarkProcessedAsync(FirstId, CancellationToken.None);

        time.Advance(TimeSpan.FromHours(1));
        Assert.True(await store.HasProcessedAsync(FirstId, CancellationToken.None));

        time.Advance(TimeSpan.FromSeconds(1));
        Assert.False(await store.HasProcessedAsync(FirstId, CancellationToken.None));
    }

    [Fact]
    public async Task ZeroRetention_DisablesDeduplication()
    {
        var store = Store(retention: TimeSpan.Zero);

        await store.MarkProcessedAsync(FirstId, CancellationToken.None);

        Assert.False(await store.HasProcessedAsync(FirstId, CancellationToken.None));
    }

    [Fact]
    public async Task Overflow_EvictsOldestMarks()
    {
        var time = new TestTimeProvider(Start);
        var store = Store(time, capacity: 2);

        await store.MarkProcessedAsync(FirstId, CancellationToken.None);
        time.Advance(TimeSpan.FromMinutes(1));
        await store.MarkProcessedAsync(SecondId, CancellationToken.None);
        time.Advance(TimeSpan.FromMinutes(1));
        await store.MarkProcessedAsync(ThirdId, CancellationToken.None);

        Assert.False(await store.HasProcessedAsync(FirstId, CancellationToken.None));
        Assert.True(await store.HasProcessedAsync(SecondId, CancellationToken.None));
        Assert.True(await store.HasProcessedAsync(ThirdId, CancellationToken.None));
    }

    [Fact]
    public async Task CancelledToken_IsObserved()
    {
        var store = Store();
        var cancelled = new CancellationToken(canceled: true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.HasProcessedAsync(FirstId, cancelled));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.MarkProcessedAsync(FirstId, cancelled));
    }

    private static InMemoryProcessedMessageStore Store(
        TestTimeProvider? time = null,
        TimeSpan? retention = null,
        int capacity = 100)
        => new(
            Options.Create(new ProcessedMessageStoreOptions
            {
                Retention = retention ?? TimeSpan.FromHours(24),
                Capacity = capacity,
            }),
            time ?? new TestTimeProvider(Start),
            NullLogger<InMemoryProcessedMessageStore>.Instance);
}

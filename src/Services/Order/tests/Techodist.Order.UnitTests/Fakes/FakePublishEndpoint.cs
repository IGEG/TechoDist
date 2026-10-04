using MassTransit;

namespace Techodist.Order.UnitTests.Fakes;

/// <summary>
/// Подменный <see cref="IPublishEndpoint"/>: запоминает опубликованные события, чтобы тесты
/// проверили состав письма (OrderSubmitted/OrderStatusChanged) и порядок относительно сохранения.
/// </summary>
internal sealed class FakePublishEndpoint(CallLog? log = null) : IPublishEndpoint
{
    private readonly List<object> _published = [];

    public IReadOnlyList<object> Published => _published;

    public IEnumerable<T> PublishedOf<T>() => _published.OfType<T>();

    /// <summary>Забыть опубликованное (например, чтобы проверить, что конфликт ничего не публикует).</summary>
    public void Reset() => _published.Clear();

    private Task Record(object message)
    {
        _published.Add(message);
        log?.Add($"publish:{message.GetType().Name}");

        return Task.CompletedTask;
    }

    public Task Publish<T>(T message, CancellationToken cancellationToken = default)
        where T : class
        => Record(message);

    public Task Publish<T>(
        T message,
        IPipe<PublishContext<T>> publishPipe,
        CancellationToken cancellationToken = default)
        where T : class
        => Publish(message, cancellationToken);

    public Task Publish<T>(object values, CancellationToken cancellationToken = default)
        where T : class
        => Record(values);

    public Task Publish<T>(
        object values,
        IPipe<PublishContext<T>> publishPipe,
        CancellationToken cancellationToken = default)
        where T : class
        => Publish<T>(values, cancellationToken);

    public Task Publish(object message, CancellationToken cancellationToken = default)
        => Record(message);

    public Task Publish(
        object message,
        IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken = default)
        => Publish(message, cancellationToken);

    public Task Publish<T>(
        T message,
        IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken = default)
        where T : class
        => Publish(message, cancellationToken);

    public Task Publish<T>(
        object values,
        IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken = default)
        where T : class
        => Publish<T>(values, cancellationToken);

    public Task Publish(object message, Type messageType, CancellationToken cancellationToken = default)
        => Publish(message, cancellationToken);

    public Task Publish(
        object message,
        Type messageType,
        IPipe<PublishContext> publishPipe,
        CancellationToken cancellationToken = default)
        => Publish(message, cancellationToken);

    // Наблюдатели публикации в тестах не нужны: MassTransit подключает их только в рантайме.
    public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => new NoopConnectHandle();

    private sealed class NoopConnectHandle : ConnectHandle
    {
        public void Disconnect() => throw new InvalidOperationException("FakePublishEndpoint: наблюдатели не поддерживаются.");

        public void Dispose()
        {
        }
    }
}

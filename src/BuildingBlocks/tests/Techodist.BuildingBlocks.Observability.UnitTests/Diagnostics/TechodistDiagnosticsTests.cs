using System.Diagnostics;
using System.Diagnostics.Metrics;
using Techodist.BuildingBlocks.Core.Diagnostics;
using Techodist.BuildingBlocks.Observability.UnitTests.Fakes;
using Xunit;

namespace Techodist.BuildingBlocks.Observability.UnitTests.Diagnostics;

/// <summary>
/// Бизнес-метрики и ручные спаны <see cref="TechodistDiagnostics"/> (ADR 0008). Инструменты
/// статические и живут на весь процесс, поэтому наблюдаем их теми же слушателями, что и
/// провайдер OpenTelemetry: <see cref="MeterListener"/> для метрик и <see cref="ActivityListener"/>
/// для трейсов. Теги проверяются «по фильтру»: значения уникальны для теста, чтобы измерения,
/// записанные другими тестами, не влияли на результат.
/// </summary>
public sealed class TechodistDiagnosticsTests
{
    [Fact]
    public void Names_ArePinnedBecauseTheyAreSubscribedFromConfiguration()
    {
        Assert.Equal("Techodist", TechodistDiagnostics.ActivitySourceName);
        Assert.Equal("Techodist", TechodistDiagnostics.MeterName);
        Assert.Equal("Techodist", TechodistDiagnostics.ActivitySource.Name);
        Assert.Equal("Techodist", TechodistDiagnostics.Meter.Name);
        Assert.False(string.IsNullOrWhiteSpace(TechodistDiagnostics.Version));
    }

    [Fact]
    public void OrderSubmitted_RecordsCounterWithCurrencyAndChannelTags()
    {
        var currency = Unique("CUR");
        var channel = Unique("channel");

        using var listener = new MetricListener();

        TechodistDiagnostics.OrderSubmitted(4321.5m, currency, channel);

        var counter = Assert.Single(listener.LongMeasurements(TechodistDiagnostics.MetricNames.OrdersSubmitted));
        Assert.Equal(1, counter.Value);
        Assert.Equal(currency, counter.Tags["currency"]);
        Assert.Equal(channel, counter.Tags["channel"]);

        var amount = Assert.Single(listener.DoubleMeasurements(TechodistDiagnostics.MetricNames.OrdersAmount));
        Assert.Equal(4321.5d, amount.Value);
        Assert.Equal(currency, amount.Tags["currency"]);
    }

    [Fact]
    public void OrderStatusChanged_RecordsStatusTransitionTags()
    {
        var from = Unique("From");
        var to = Unique("To");

        using var listener = new MetricListener();

        TechodistDiagnostics.OrderStatusChanged(from, to);

        var measurement = Assert.Single(
            listener.LongMeasurements(TechodistDiagnostics.MetricNames.OrderStatusChanges));

        Assert.Equal(1, measurement.Value);
        Assert.Equal(from, measurement.Tags["from_status"]);
        Assert.Equal(to, measurement.Tags["to_status"]);
    }

    [Fact]
    public void BasketItemAdded_IncrementsByQuantityWithoutTags()
    {
        using var listener = new MetricListener();

        TechodistDiagnostics.BasketItemAdded(3);

        var measurement = Assert.Single(
            listener.LongMeasurements(TechodistDiagnostics.MetricNames.BasketItemsAdded));

        Assert.Equal(3, measurement.Value);
        Assert.Empty(measurement.Tags);
    }

    [Fact]
    public void ProductPublished_CountsOnePerPublication()
    {
        using var listener = new MetricListener();

        TechodistDiagnostics.ProductPublished();
        TechodistDiagnostics.ProductPublished();

        var measurements = listener.LongMeasurements(TechodistDiagnostics.MetricNames.ProductsPublished);

        Assert.Equal(2, measurements.Sum(measurement => measurement.Value));
    }

    [Fact]
    public void NotificationSentAndFailed_AreSeparatedByKind()
    {
        var kind = Unique("kind");

        using var listener = new MetricListener();

        TechodistDiagnostics.NotificationSent(kind);
        TechodistDiagnostics.NotificationFailed(kind);

        var sent = Assert.Single(listener.LongMeasurements(TechodistDiagnostics.MetricNames.NotificationsSent));
        var failed = Assert.Single(listener.LongMeasurements(TechodistDiagnostics.MetricNames.NotificationsFailed));

        Assert.Equal(1, sent.Value);
        Assert.Equal(1, failed.Value);
        Assert.Equal(kind, sent.Tags["kind"]);
        Assert.Equal(kind, failed.Tags["kind"]);
    }

    [Fact]
    public void StartActivity_CreatesSpanWithTagsWhenSourceIsSubscribed()
    {
        var listener = TestActivityListener.ListenTo(
            source => source.Name == TechodistDiagnostics.ActivitySourceName);

        try
        {
            using var activity = TechodistDiagnostics.StartActivity(TechodistDiagnostics.ActivityNames.OrderSubmit);

            Assert.NotNull(activity);
            Assert.Equal(TechodistDiagnostics.ActivityNames.OrderSubmit, activity!.DisplayName);
            Assert.Equal(ActivityKind.Internal, activity.Kind);
            Assert.Equal(TechodistDiagnostics.ActivitySourceName, activity.Source.Name);

            activity.SetTag("order.currency", "RUB");

            Assert.Equal("RUB", activity.GetTagItem("order.currency"));
        }
        finally
        {
            TestActivityListener.StopListening(listener);
        }
    }

    [Fact]
    public void StartActivity_ReturnsNullWhenNobodyListens()
    {
        // Слушателей у источника быть не должно: хост с OpenTelemetry-провайдером (тест-хост)
        // обязан освобождаться, иначе его подписка на «Techodist» живёт до конца процесса.
        Assert.False(
            TechodistDiagnostics.ActivitySource.HasListeners(),
            "Предыдущий тест оставил подписку на TechodistDiagnostics: освобождайте хост через IAsyncDisposable.");

        // Никто не подписан — активность не создаётся, и вызывающий код обязан работать с null.
        Assert.Null(TechodistDiagnostics.StartActivity(TechodistDiagnostics.ActivityNames.OrderSubmit));
    }

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    /// <summary>Слушатель <see cref="Meter"/>: собирает измерения инструментов Techodist с тегами.</summary>
    private sealed class MetricListener : IDisposable
    {
        private readonly MeterListener _listener = new();

        private readonly List<Measurement> _longMeasurements = [];

        private readonly List<Measurement> _doubleMeasurements = [];

        public MetricListener()
        {
            _listener.InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == TechodistDiagnostics.MeterName)
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            };

            _listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
                _longMeasurements.Add(Measurement.Create(instrument.Name, value, tags)));

            _listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
                _doubleMeasurements.Add(Measurement.Create(instrument.Name, value, tags)));

            _listener.Start();
        }

        public IReadOnlyList<Measurement> LongMeasurements(string instrumentName) =>
            [.. _longMeasurements.Where(measurement => measurement.Instrument == instrumentName)];

        public IReadOnlyList<Measurement> DoubleMeasurements(string instrumentName) =>
            [.. _doubleMeasurements.Where(measurement => measurement.Instrument == instrumentName)];

        public void Dispose() => _listener.Dispose();
    }

    private sealed record Measurement(string Instrument, double Value, IReadOnlyDictionary<string, object?> Tags)
    {
        public static Measurement Create(
            string instrument,
            double value,
            ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            var dictionary = new Dictionary<string, object?>(StringComparer.Ordinal);

            foreach (var tag in tags)
            {
                dictionary[tag.Key] = tag.Value;
            }

            return new Measurement(instrument, value, dictionary);
        }
    }
}

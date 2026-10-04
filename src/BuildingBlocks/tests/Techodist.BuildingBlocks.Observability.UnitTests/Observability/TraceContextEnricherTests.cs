using System.Diagnostics;
using Serilog.Events;
using Serilog.Parsing;
using Techodist.BuildingBlocks.Observability.UnitTests.Fakes;
using Xunit;

namespace Techodist.BuildingBlocks.Observability.UnitTests.Observability;

/// <summary>
/// Обогащение логов идентификаторами трейса (ADR 0008): по <c>TraceId</c> запись из Loki
/// находится в Jaeger, а <c>SpanId</c> указывает на конкретный спан.
/// </summary>
public sealed class TraceContextEnricherTests
{
    private readonly TraceContextEnricher _enricher = new();

    [Fact]
    public void Enrich_WithW3CActivity_AddsTraceAndSpanIds()
    {
        var listener = TestActivityListener.ListenTo(_ => true);

        try
        {
            using var source = new ActivitySource("Techodist.Tests.Enricher");
            using var activity = source.StartActivity("order.submit");

            Assert.NotNull(activity);
            Assert.Equal(ActivityIdFormat.W3C, activity!.IdFormat);

            var logEvent = CreateLogEvent();

            _enricher.Enrich(logEvent, new TestPropertyFactory());

            Assert.Equal(
                activity.TraceId.ToHexString(),
                Assert.IsType<ScalarValue>(logEvent.Properties[TraceContextEnricher.TraceIdPropertyName]).Value);

            Assert.Equal(
                activity.SpanId.ToHexString(),
                Assert.IsType<ScalarValue>(logEvent.Properties[TraceContextEnricher.SpanIdPropertyName]).Value);
        }
        finally
        {
            TestActivityListener.StopListening(listener);
        }
    }

    [Fact]
    public void Enrich_WithoutActivity_LeavesEventUntouched()
    {
        var logEvent = CreateLogEvent();

        _enricher.Enrich(logEvent, new TestPropertyFactory());

        // Фоновые записи (старт сервиса, задачи без спана) не должны обрастать пустыми идентификаторами.
        Assert.False(logEvent.Properties.ContainsKey(TraceContextEnricher.TraceIdPropertyName));
        Assert.False(logEvent.Properties.ContainsKey(TraceContextEnricher.SpanIdPropertyName));
    }

    [Fact]
    public void Enrich_DoesNotOverwriteExistingProperties()
    {
        var listener = TestActivityListener.ListenTo(_ => true);

        try
        {
            using var source = new ActivitySource("Techodist.Tests.Enricher");
            using var activity = source.StartActivity("order.submit");

            Assert.NotNull(activity);

            var logEvent = CreateLogEvent();

            // Например, идентификатор пришёл в сообщении из брокера и уже записан в лог-контекст.
            logEvent.AddPropertyIfAbsent(new LogEventProperty(
                TraceContextEnricher.TraceIdPropertyName,
                new ScalarValue("existing-trace")));

            _enricher.Enrich(logEvent, new TestPropertyFactory());

            Assert.Equal(
                "existing-trace",
                Assert.IsType<ScalarValue>(logEvent.Properties[TraceContextEnricher.TraceIdPropertyName]).Value);
        }
        finally
        {
            TestActivityListener.StopListening(listener);
        }
    }

    [Fact]
    public void Enrich_NullArguments_AreRejected()
    {
        var propertyFactory = new TestPropertyFactory();

        Assert.Throws<ArgumentNullException>(() => _enricher.Enrich(null!, propertyFactory));
        Assert.Throws<ArgumentNullException>(() => _enricher.Enrich(CreateLogEvent(), null!));
    }

    private static LogEvent CreateLogEvent() => new(
        DateTimeOffset.UtcNow,
        LogEventLevel.Information,
        exception: null,
        new MessageTemplateParser().Parse("Тестовое событие"),
        properties: []);
}

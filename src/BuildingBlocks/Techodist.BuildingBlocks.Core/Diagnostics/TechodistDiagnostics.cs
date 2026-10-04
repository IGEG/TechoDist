using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;

namespace Techodist.BuildingBlocks.Core.Diagnostics;

/// <summary>
/// Точка входа в телеметрию приложения (ADR 0008): <see cref="ActivitySource"/> для трейсов и
/// <see cref="Meter"/> для метрик. Живёт в Core и не тянет внешних зависимостей: провайдеры
/// (OpenTelemetry) подключаются в BuildingBlocks.Observability по именам
/// <see cref="ActivitySourceName"/> и <see cref="MeterName"/>, а метрики попадают в Prometheus.
/// </summary>
/// <remarks>
/// Инструменты статичны осознанно: у <see cref="Meter"/>/<see cref="ActivitySource"/> нет состояния,
/// которое нужно подменять, а измерения привязаны к процессу, а не к запросу (DI-обёртка добавила бы
/// шум во все слои, включая домен). Юнит-тесты наблюдают инструменты через <see cref="MeterListener"/>.
/// </remarks>
public static class TechodistDiagnostics
{
    /// <summary>Имя <see cref="ActivitySource"/> — его подписывает OpenTelemetry-трейсер.</summary>
    public const string ActivitySourceName = "Techodist";

    /// <summary>Имя <see cref="Meter"/> — его слушает OpenTelemetry-провайдер метрик.</summary>
    public const string MeterName = "Techodist";

    /// <summary>Версия сборки: уезжает в ресурс <c>service.version</c>.</summary>
    public static string Version { get; } = ResolveVersion();

    /// <summary>Активности ручной инструментации (спаны) — трейсы склеиваются с ASP.NET Core/HTTP.</summary>
    public static ActivitySource ActivitySource { get; } = new(ActivitySourceName, Version);

    /// <summary>Метр бизнес-метрик — их скрейпит Prometheus через OpenTelemetry.</summary>
    public static Meter Meter { get; } = new(MeterName, Version);

    private const string ChannelTag = "channel";
    private const string CurrencyTag = "currency";
    private const string FromStatusTag = "from_status";
    private const string ToStatusTag = "to_status";
    private const string NotificationKindTag = "kind";

    /// <summary>Имена инструментов (в Prometheus точки меняются на <c>_</c>, счётчики получают <c>_total</c>).</summary>
    public static class MetricNames
    {
        public const string OrdersSubmitted = "techodist.orders.submitted";
        public const string OrdersAmount = "techodist.orders.amount";
        public const string OrderStatusChanges = "techodist.orders.status_changes";
        public const string BasketItemsAdded = "techodist.basket.items_added";
        public const string ProductsPublished = "techodist.catalog.products.published";
        public const string NotificationsSent = "techodist.notifications.sent";
        public const string NotificationsFailed = "techodist.notifications.failed";
    }

    /// <summary>Имена активностей ручной инструментации.</summary>
    public static class ActivityNames
    {
        public const string OrderSubmit = "order.submit";
        public const string OrderStatusChange = "order.status_change";
        public const string NotificationSend = "notification.send";
    }

    /// <summary>Значения тега <c>kind</c> у метрик уведомлений.</summary>
    public static class NotificationKinds
    {
        public const string StoreSubmitted = "store_submitted";
        public const string CustomerConfirmation = "customer_confirmation";
        public const string CustomerStatusChange = "customer_status_change";
    }

    private static readonly Counter<long> OrdersSubmittedCounter = Meter.CreateCounter<long>(
        MetricNames.OrdersSubmitted,
        unit: "{order}",
        description: "Число оформленных заявок.");

    private static readonly Histogram<double> OrdersAmountHistogram = Meter.CreateHistogram<double>(
        MetricNames.OrdersAmount,
        description: "Сумма оформленной заявки (валюта — в теге currency).");

    private static readonly Counter<long> OrderStatusChangesCounter = Meter.CreateCounter<long>(
        MetricNames.OrderStatusChanges,
        unit: "{change}",
        description: "Число смен статуса заявки менеджером.");

    private static readonly Counter<long> BasketItemsAddedCounter = Meter.CreateCounter<long>(
        MetricNames.BasketItemsAdded,
        unit: "{item}",
        description: "Число позиций, добавленных в гостевую корзину.");

    private static readonly Counter<long> ProductsPublishedCounter = Meter.CreateCounter<long>(
        MetricNames.ProductsPublished,
        unit: "{product}",
        description: "Число опубликованных товаров каталога.");

    private static readonly Counter<long> NotificationsSentCounter = Meter.CreateCounter<long>(
        MetricNames.NotificationsSent,
        unit: "{email}",
        description: "Число отправленных писем Notification.");

    private static readonly Counter<long> NotificationsFailedCounter = Meter.CreateCounter<long>(
        MetricNames.NotificationsFailed,
        unit: "{email}",
        description: "Число писем, которые не удалось отправить (SMTP-сбой).");

    /// <summary>
    /// Запускает активность ручной инструментации (спан). Возвращает <c>null</c>, если трейсинг
    /// выключен, поэтому результат всегда nullable. Активность сама встаёт в текущий трейс,
    /// если он есть (например, span ASP.NET Core запроса или обработки сообщения брокера).
    /// </summary>
    public static Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
        => ActivitySource.StartActivity(name, kind);

    /// <summary>Фиксирует оформленную заявку: счётчик заявок и гистограмму суммы.</summary>
    public static void OrderSubmitted(decimal amount, string currency, string channel)
    {
        var tags = new TagList
        {
            { CurrencyTag, currency },
            { ChannelTag, channel },
        };

        OrdersSubmittedCounter.Add(1, tags);
        OrdersAmountHistogram.Record((double)amount, tags);
    }

    /// <summary>Фиксирует смену статуса заявки менеджером.</summary>
    public static void OrderStatusChanged(string fromStatus, string toStatus)
        => OrderStatusChangesCounter.Add(
            1,
            new TagList
            {
                { FromStatusTag, fromStatus },
                { ToStatusTag, toStatus },
            });

    /// <summary>Фиксирует добавленные в корзину позиции: счётчик растёт на <paramref name="quantity"/>.</summary>
    public static void BasketItemAdded(int quantity)
        => BasketItemsAddedCounter.Add(quantity);

    /// <summary>
    /// Фиксирует публикацию товара. Категория в теги не выносится осознанно: справочник растёт,
    /// а новые значения тегов у счётчика порождают новые временные ряды в Prometheus.
    /// </summary>
    public static void ProductPublished()
        => ProductsPublishedCounter.Add(1);

    /// <summary>Фиксирует успешную отправку письма (<paramref name="kind"/> — <see cref="NotificationKinds"/>).</summary>
    public static void NotificationSent(string kind)
        => NotificationsSentCounter.Add(1, new TagList { { NotificationKindTag, kind } });

    /// <summary>Фиксирует неудачную отправку письма: SMTP-сбой, сообщение будет повторено брокером.</summary>
    public static void NotificationFailed(string kind)
        => NotificationsFailedCounter.Add(1, new TagList { { NotificationKindTag, kind } });

    private static string ResolveVersion() =>
        typeof(TechodistDiagnostics).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(TechodistDiagnostics).Assembly.GetName().Version?.ToString()
        ?? "unknown";
}

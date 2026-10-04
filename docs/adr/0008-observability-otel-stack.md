# ADR 0008. Наблюдаемость: OpenTelemetry + Grafana stack

- **Статус:** Accepted (реализовано в Фазе 9)
- **Дата:** 2026-04-10

## Контекст

Нужно просматривать логи и метрики (требование), а для учебного уровня — ещё и
трейсы. Всё должно быть бесплатным и доступным в РФ.

## Решение

- Инструментирование сервисов — **OpenTelemetry .NET** (traces, metrics, logs).
- Метрики — **Prometheus** + **Grafana**.
- Логи — **Serilog** → **Loki** (+ Promtail), просмотр в Grafana.
- Трейсы — через **OpenTelemetry Collector** → **Jaeger**.
- Алерты — **Alertmanager**.

## Последствия

- (+) Единый стандарт (Otel), нет вендор-локина, всё open-source.
- (+) Коррелируются метрики/логи/трейсы по `traceId`.
- (−) Дополнительные контейнеры и ресурсы (в dev запускаем по мере необходимости).
- Elasticsearch/Kibana используются для поиска товаров; для логов выбран Loki,
  чтобы развести зоны ответственности и показать оба стека.

## Реализация (Фаза 9)

- **Библиотеки.** `BuildingBlocks.Core/Diagnostics/TechodistDiagnostics.cs` — статические
  `ActivitySource`/`Meter` (`Techodist`), бизнес-метрики и хелперы спанов; `BuildingBlocks.Observability`
  — `ObservabilityExtensions` (`AddTechodistObservability`/`UseTechodistObservability`),
  `ObservabilityOptions` (секция `Observability`, дефолты рассчитаны на локальный стек),
  `TraceContextEnricher` (`TraceId`/`SpanId` в логах); `BuildingBlocks.Web/Health` — общий
  JSON-writer отчёта и эндпоинты `/health/live`/`/health/ready` для шлюза и сервисов.
- **Сервисы.** Во всех 7 `Program.cs` вызываются `AddTechodistObservability("<service>")` и
  `UseTechodistObservability()`; адреса стека — в секции `Observability` каждого
  `appsettings.Development.json`.
- **Бизнес-инструментация.** Order — спаны `order.submit`/`order.status_change` плюс счётчик заявок
  и гистограмма суммы (валюта/канал в тегах); Basket — добавленные позиции; Catalog — публикации
  товаров; Notification — `notification.send` и счётчики успешных/неудачных писем (`EmailTelemetry`).
- **Инфраструктура.** `deploy/docker-compose/docker-compose.observability.yml` + конфиги в
  `deploy/observability/**`: Collector -> Jaeger, Prometheus (скрейп `host.docker.internal:5100…5106`,
  6 правил алертов) -> Alertmanager, Loki (Promtail — для логов контейнеров), Grafana с провижном
  (3 datasource, 2 дашборда).
- **Дашборды.** «Techodist · Сервисы (RED)» — доступность, трафик, доля 5xx, p50/p95/p99,
  runtime .NET и логи; «Techodist · Бизнес-метрики» — заявки, средний чек, каналы связи, смены
  статусов, корзина, публикации товаров, письма и ошибки из Loki.
- **Найденные ограничения.** Prometheus-экспортёр OpenTelemetry не кладёт в ряд идентификатор
  сервиса (ни `service_name`, ни `target_info`), поэтому имя сервиса проставляет сам скрейп
  (`static_configs.labels.service`), а алерты и дашборды фильтруют по этому лейблу. Логи идут
  напрямую Serilog -> Loki; Promtail собирает только stdout контейнеров.
- **Проверка.** Юнит-тесты наблюдаемости (`MeterListener`/`ActivityListener`, 25 тестов) плюс
  валидация конфигов в CI-джобе `deploy-configs` (`docker compose config`, `promtool check
  config/rules`, `amtool check-config`, `otelcol validate`, `loki -verify-config`,
  `promtail -check-syntax`, JSON дашбордов). На живом стенде проверены метрики с лейблом `service`,
  `TraceId` в записях Loki, трейсы в Jaeger и всплытие алерта в Alertmanager.

# Документация — Techodist

Магазин оборудования для регенерации растворителей (учебный enterprise-проект).

- **[architecture.md](architecture.md)** — полное описание архитектуры системы.
- **[adr/](adr/)** — реестр архитектурных решений (Architecture Decision Records).
- Описания сервисов: [Catalog](../src/Services/Catalog/README.md),
  [Basket](../src/Services/Basket/README.md), [Order](../src/Services/Order/README.md),
  [Notification](../src/Services/Notification/README.md),
  [Search](../src/Services/Search/README.md),
  [фронтенд (SPA)](../src/Frontend/README.md),
  [docker-compose (инфраструктура)](../deploy/docker-compose/README.md),
  [наблюдаемость (Prometheus/Grafana/Loki/Jaeger)](../deploy/observability/README.md).

## Наблюдаемость

Стек наблюдаемости (ADR 0008) живёт в [deploy/observability](../deploy/observability/README.md):
метрики — Prometheus + Grafana, логи — Serilog → Loki, трейсы — OpenTelemetry Collector → Jaeger,
алерты — Alertmanager. Два дашборда провижнятся из `deploy/observability/grafana/dashboards`
(«Сервисы (RED)» и «Бизнес-метрики»), правила алертов — `deploy/observability/prometheus/alerts.yml`,
точки инструментирования — `TechodistDiagnostics` (`ActivitySource`/`Meter` = `Techodist`).

## Быстрая навигация по решениям (ADR)

| № | Решение |
|---|---------|
| 0001 | Микросервисы + Clean Architecture |
| 0002 | Database-per-service (PostgreSQL на каждый сервис) |
| 0003 | RabbitMQ + MassTransit, Outbox |
| 0004 | OpenIddict, авторизация только для админки |
| 0005 | Анонимная корзина: Redis + HttpOnly-cookie |
| 0006 | React + TS + Vite, Tailwind CSS + shadcn/ui |
| 0007 | YARP как API Gateway |
| 0008 | Наблюдаемость: OpenTelemetry + Prometheus/Grafana/Loki/Jaeger |
| 0009 | Search Service: Elasticsearch как индекс каталога |
| 0010 | Фронтенд витрины и админки: серверная корзина, кэш, продление токена |

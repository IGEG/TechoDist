# Документация — Techodist

Магазин оборудования для регенерации растворителей (учебный enterprise-проект).

- **[architecture.md](architecture.md)** — полное описание архитектуры системы.
- **[adr/](adr/)** — реестр архитектурных решений (Architecture Decision Records).
- Описания сервисов: [Catalog](../src/Services/Catalog/README.md),
  [Basket](../src/Services/Basket/README.md), [Order](../src/Services/Order/README.md),
  [Notification](../src/Services/Notification/README.md),
  [Search](../src/Services/Search/README.md),
  [docker-compose (инфраструктура)](../deploy/docker-compose/README.md).

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

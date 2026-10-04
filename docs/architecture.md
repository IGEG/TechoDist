# Архитектура EcoTech

> Домен: интернет-магазин оборудования для очистки/регенерации растворителей
> (по мотивам ecotech-ngt.ru). **Онлайн-оплаты нет** — оформление заявки
> отправляется на e-mail магазина.

## 1. Контекст и цели

- Клиент-серверное приложение уровня enterprise, микросервисная архитектура.
- Backend — ASP.NET Core (.NET 9), Frontend — React + TS + Vite.
- Только бесплатные/open-source компоненты, доступные в РФ.
- Учебный проект: используется широкий набор систем (даже если избыточно).
- Наблюдаемость: логи, метрики, трейсы.
- В дальнейшем разворачивается на виртуальном сервере под Kubernetes (k3s).

## 2. Компоненты (сервисы)

| Сервис | Ответственность | Хранилище |
|--------|-----------------|-----------|
| **ApiGateway** | Единая точка входа (YARP): маршрутизация, rate-limit, JWT, CORS | — |
| **Identity** | Аутентификация админов/менеджеров (OpenIddict), роли, JWT | PostgreSQL |
| **Catalog** | Товары, категории, марки растворителей, характеристики, цены | PostgreSQL + Redis (кэш) |
| **Basket** | Гостевая корзина (анонимный `basketId`) | Redis |
| **Order** | Оформление заявки, статусы, outbox | PostgreSQL |
| **Notification** | Отправка писем (заявка магазину + подтверждение клиенту) | — |
| **Search** | Полнотекстовый поиск по товарам | Elasticsearch |

## 3. Слои сервиса (Clean Architecture)

```
Service.Api              -> Presentation: Minimal API/Controllers, OpenAPI, health, DI
Service.Application      -> Use cases: команды/запросы (MediatR), валидация, DTO
Service.Domain           -> Сущности, агрегаты, value objects, доменные события
Service.Infrastructure   -> EF Core, Redis, RabbitMQ, SMTP, внешние клиенты
tests/Service.*.Tests    -> Unit + Integration (Testcontainers)
```

## 4. Ключевые потоки

### 4.1 Оформление заявки (без оплаты)

```
Гость -> Каталог -> «В корзину» -> Basket(Redis) + cookie basket-id
      -> Корзина -> форма контактов (honeypot + rate-limit на Gateway)
      -> Order: status=Pending, запись в outbox
      -> RabbitMQ: событие OrderSubmitted
      -> Notification (MailKit/SMTP): письмо магазину + подтверждение клиенту
      -> Админка: менеджер меняет статусы -> уведомления клиенту
```

### 4.2 Синхронизация поиска

```
Catalog -> доменные события (товар создан/изменён/удалён)
        -> RabbitMQ -> Search: обновление индекса Elasticsearch
```

## 5. Наблюдаемость

| Слой | Инструмент |
|------|------------|
| Метрики | Prometheus + Grafana |
| Логи | Serilog -> Loki + Promtail (просмотр в Grafana) |
| Трейсы | OpenTelemetry -> OpenTelemetry Collector -> Jaeger |
| Алерты | Alertmanager |

Каждый сервис экспортирует `/metrics`, `/health/live`, `/health/ready`, а также
трассирует входящие/исходящие вызовы (aspnetcore, http, EF Core, MassTransit).

## 6. Развёртывание

- **Dev**: docker-compose (инфраструктура) + сервисы из IDE.
- **Prod**: Kubernetes (k3s) + Helm, Nginx Ingress + cert-manager (Let's Encrypt).
- **CI/CD**: GitHub Actions (build/test) + образы в registry; GitOps-опция — Argo CD.

## 7. Безопасность

- OpenIddict + JWT (access/refresh), роли `Admin` (и при необходимости `Manager`).
- HTTPS через cert-manager. Rate-limiting публичных форм на Gateway.
- Валидация входа (FluentValidation), единый формат ошибок (ProblemDetails).
- Секреты — через K8s Secrets / user-secrets / `.env` (не в git).

## 8. Связанные решения

Детали и обоснования — в [adr/](adr/).

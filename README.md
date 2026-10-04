# Techodist — интернет-магазин оборудования для регенерации растворителей

Учебный проект уровня enterprise: клиент-серверное приложение на микросервисной
архитектуре. Домен — продажа установок для очистки/регенерации растворителей.
Оборудование выпускается под торговой маркой **Techodist** (кратко **TD**): номер модели
соответствует объёму установки — **TD20** (20 л), **TD60** (60 л), **TD120** (120 л),
вакуумное исполнение — **TDV40**. **Онлайн-оплаты нет**: оформление заявки отправляется
на e-mail магазина.

## Стек

| Слой | Технологии |
|------|------------|
| Backend | ASP.NET Core (.NET 9), Clean Architecture, DDD, CQRS (MediatR), EF Core + PostgreSQL |
| Messaging | RabbitMQ + MassTransit (outbox, события) |
| Cache | Redis |
| Identity | OpenIddict + ASP.NET Core Identity (только админ-панель) |
| Поиск | Elasticsearch |
| Frontend | React + TypeScript + Vite, Tailwind CSS + shadcn/ui, TanStack Query, Zustand |
| API Gateway | YARP |
| Наблюдаемость | OpenTelemetry, Prometheus, Grafana, Loki, Jaeger |
| Инфра | Docker, Kubernetes (k3s), Helm, Nginx Ingress, cert-manager |
| CI/CD | GitHub Actions |

Полное описание архитектуры — [docs/architecture.md](docs/architecture.md).
Реестр архитектурных решений — [docs/adr/](docs/adr/).

## Структура репозитория

```
progs/
├─ docs/                 # архитектура и ADR
├─ src/
│  ├─ BuildingBlocks/    # общие библиотеки (Core, Messaging, Observability, Web)
│  ├─ Services/          # микросервисы (Catalog, Identity, Basket, Order, Notification, Search)
│  ├─ ApiGateway/        # YARP
│  └─ Frontend/          # React + TS + Vite
├─ deploy/               # docker-compose, helm, k8s
└─ .github/workflows/    # CI/CD
```

## Быстрый старт (локальная инфраструктура)

```powershell
# 1. Скопировать env
Copy-Item .env.example .env

# 2. Поднять инфраструктуру (БД, Redis, RabbitMQ, Elasticsearch, MailHog)
docker compose -f deploy/docker-compose/docker-compose.infrastructure.yml up -d

# 3. Собрать backend
dotnet build Techodist.sln
```

Сервисы инфраструктуры и порты: см. [deploy/docker-compose/README.md](deploy/docker-compose/README.md).

## Статус работ

- [x] Фаза 0 — подготовка окружения и ADR
- [x] Фаза 1 — скелет монорепо + BuildingBlocks + docker-compose инфраструктуры
- [x] Фаза 2 — Catalog Service (эталонный вертикальный срез: Domain/Application/Infrastructure/Api + миграция + 19 unit-тестов)
- [ ] Фаза 3 — Identity Service (OpenIddict)
- [ ] Фаза 4 — API Gateway (YARP) + скелет фронтенда
- [ ] Фаза 5 — Basket Service
- [ ] Фаза 6 — Order + Notification + RabbitMQ
- [ ] Фаза 7 — Search Service (Elasticsearch)
- [ ] Фаза 8 — Полный фронтенд
- [ ] Фаза 9 — Наблюдаемость
- [ ] Фаза 10 — Kubernetes (k3s)
- [ ] Фаза 11 — Полировка

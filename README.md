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

## Запуск сервисов (dev)

Порт задаётся в `launchSettings.json` сервиса, строка подключения к БД — в его
`appsettings.Development.json`. Секреты (`Jwt:SigningKey`, пароль администратора) локально
берутся из `appsettings.Development.json`, а вне Development — из User-Secrets / переменных
окружения (`Jwt__SigningKey`, `Bootstrap__AdminPassword`, `OpenIddict__AdminClientSecret`).

```powershell
dotnet run --project src/Services/Catalog/Techodist.Catalog.Api             # http://localhost:5101
dotnet run --project src/Services/Identity/Techodist.Identity.Api           # http://localhost:5102
dotnet run --project src/Services/Basket/Techodist.Basket.Api               # http://localhost:5103
dotnet run --project src/Services/Order/Techodist.Order.Api                 # http://localhost:5104
dotnet run --project src/Services/Notification/Techodist.Notification.Api   # http://localhost:5105
dotnet run --project src/ApiGateway/Techodist.ApiGateway                    # http://localhost:5100 — единая точка входа (YARP)
```

Фронтенд и внешние клиенты ходят только через шлюз: он публикует маршруты
`/catalog/...`, `/identity/...`, `/basket/...` и `/order/...` и снимает префикс сервиса перед
проксированием (см. `ReverseProxy` в `src/ApiGateway/Techodist.ApiGateway/appsettings.json`).
Basket дополнительно требует запущенного Redis (`localhost:6379`) и доступного Catalog —
снимок цены и названия берётся синхронно (см. [src/Services/Basket/README.md](src/Services/Basket/README.md)).
Order требует запущенного Basket (позиции заявки) и RabbitMQ (события через outbox), а
Notification — RabbitMQ и SMTP (в dev — MailHog `localhost:1025`, письма видны на
http://localhost:8025). Детали — [Order](src/Services/Order/README.md) и
[Notification](src/Services/Notification/README.md).

Учётка администратора создаётся сидером при первом старте Identity:
`admin@techodist.local` / `Techodist!Dev2026` (Development). Получить access-токен для
админских эндпоинтов Catalog и Order (важно запросить нужный **scope = аудитория** сервиса:
`techodist-catalog-api` или `techodist-order-api`):

```powershell
curl -X POST http://localhost:5102/connect/token -d "grant_type=password&client_id=techodist-admin-panel&client_secret=techodist-admin-panel-dev-secret&username=admin@techodist.local&password=Techodist!Dev2026&scope=techodist-catalog-api"
```

Полученный `access_token` передаётся в заголовке `Authorization: Bearer <token>`.
Через шлюз тот же endpoint доступен как `http://localhost:5100/identity/connect/token`
(маршрут `identity-token` с rate-limit) — именно так запрашивает токен фронтенд.
Подробности потока (пароль/refresh, аудитории, время жизни, подпись) —
[ADR 0004](docs/adr/0004-openiddict-admin-only.md).

## Запуск фронтенда (dev)

SPA на Vite (порт 5173). Наружу ходит только в шлюз: адрес API задаётся переменной
`VITE_API_BASE_URL` (по умолчанию `http://localhost:5100`), префикс сервиса
(`/catalog/api`, `/identity/connect`, `/basket/api`) добавляется к запросу.
Запросы идут с `withCredentials: true` — иначе браузер не отправит HttpOnly-cookie
гостевой корзины через шлюз (ADR 0005).

```powershell
cd src/Frontend
Copy-Item .env.example .env.local   # адрес шлюза + dev-клиент админ-панели
npm install
npm run dev                         # http://localhost:5173
```

| Маршрут | Страница |
|---------|----------|
| `/` | редирект в каталог |
| `/catalog` | каталог: поиск, фильтры категории/цены, сортировка, пагинация (состояние в query-string — ссылка на выдачу делимая) |
| `/catalog/:productId` | карточка товара |
| `/admin/login` | вход сотрудника (OpenIddict password grant) |
| `/admin` | админ-панель; роли `Admin`/`Manager`, страница грузится отдельным чанком |

Проверка качества (то же запускает CI):

```powershell
npm run lint    # ESLint (flat config)
npm run test    # vitest + Testing Library + MSW
npm run build   # tsc -b + vite build
```

`VITE_ADMIN_CLIENT_SECRET` виден браузеру, поэтому допустим только в dev-сценарии
(confidential-клиент админ-панели); для прода нужен BFF/прокси — см.
[ADR 0004](docs/adr/0004-openiddict-admin-only.md).

## Статус работ

- [x] Фаза 0 — подготовка окружения и ADR
- [x] Фаза 1 — скелет монорепо + BuildingBlocks + docker-compose инфраструктуры
- [x] Фаза 2 — Catalog Service (эталонный вертикальный срез: Domain/Application/Infrastructure/Api + миграция + 19 unit-тестов)
- [x] Фаза 3 — Identity Service (OpenIddict: вход админа по паролю + refresh-токен, CRUD админ-пользователей, JWT-защита админских операций Catalog; 74 unit-теста)
- [x] Фаза 4 — API Gateway (YARP): проксирование Catalog/Identity, JWT-авторизация админских маршрутов, rate-limit, CORS, агрегация health (15 unit-тестов) + фронтенд: публичный каталог с фильтрами/пагинацией, карточка товара, вход в админ-панель и её каркас (40 vitest-тестов)
- [x] Фаза 5 — Basket Service (гостевая корзина в Redis: анонимный HttpOnly-cookie `basketId`, CQRS-сценарии корзины, снимок товара из Catalog, health-check Redis; 66 unit-тестов Basket + 2 unit-теста маршрута `basket-api` в шлюзе + 4 vitest-теста фронтенда на cookie корзины)
- [x] Фаза 6 — Order + Notification + RabbitMQ (заявка из гостевой корзины: номер `TD-ГГГГММДД-00042`, воронка статусов `Pending → Confirmed → InProgress → Completed/Cancelled`, transactional outbox и события `OrderSubmitted`/`OrderStatusChanged`, письма магазину и клиенту в Notification с дедупликацией по `MessageId`; 111 unit-тестов Order + 36 unit-тестов Notification + 18 unit-тестов шлюза (включая разбор гостевых и админских маршрутов `order-api`))
- [ ] Фаза 7 — Search Service (Elasticsearch)
- [ ] Фаза 8 — Полный фронтенд (админ-CRUD каталога, корзина и оформление заявки)
- [ ] Фаза 9 — Наблюдаемость
- [ ] Фаза 10 — Kubernetes (k3s)
- [ ] Фаза 11 — Полировка

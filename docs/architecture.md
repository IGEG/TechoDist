# Архитектура Techodist

> Домен: интернет-магазин оборудования для очистки/регенерации растворителей.
> Оборудование — под торговой маркой **Techodist** (кратко **TD**), модели по объёму
> установки: TD20 (20 л), TD60 (60 л), TD120 (120 л), вакуумная TDV40.
> **Онлайн-оплаты нет** — оформление заявки отправляется на e-mail магазина.

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
| **Order** | Оформление заявки (номер `TD-…`), воронка статусов, outbox | PostgreSQL |
| **Notification** | Письма: заявка магазину, подтверждение и смена статуса клиенту | — |
| **Search** | Полнотекстовый поиск по товарам (индекс — проекция каталога) | Elasticsearch |

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

- Гостевой вход защищён двумя механизмами: honeypot на витрине и rate-limit шлюза
  (`order-submit`). Заявку создаёт только `POST /order/api/orders`, статус читается по номеру
  из письма (`GET /order/api/orders/number/{number}`) — токен для этого не нужен.
- Заявка и событие коммитятся вместе (outbox), а письма отправляет Notification по событию:
  «заявка без письма» возможна только при недоступном SMTP — брокер повторит доставку.
- Подробности — в разделе [4.6](#46-заявки-и-уведомления-adr-0003).

### 4.2 Синхронизация поиска (ADR 0009)

```
Каталог -> сохранение товара + доменное событие -> outbox (одной транзакцией, ADR 0003)
        -> RabbitMQ: ProductChangedIntegrationEvent (снимок карточки)
        -> Search: ProductChangedConsumer -> upsert/delete документа в индексе Elasticsearch
Запуск/расписание (30 мин) -> Search: чтение публичного Catalog API
        -> CatalogIndexReconciler: EnsureIndexAsync + upsert опубликованных товаров
Витрина -> Gateway /search/api/search/products -> Search -> Elasticsearch
```

- Индекс — **read-модель** каталога: своей БД у Search нет, поэтому нет ни миграций, ни outbox.
  Источник истины — Catalog, индекс полностью восстановим (удалить индекс и дать сервису
  наполнить его заново).
- Обычный путь — событие `ProductChanged` из outbox каталога; снимок карточки едет внутри
  сообщения, поэтому в момент индексации Search не обращается к Catalog (как Order → Notification).
- Реконсиляция — страховка от потерянных событий (сервис был недоступен, индекс поднят с нуля):
  при старте и по расписанию индекс догоняет каталог публичным read-API.
- В индексе живут только опубликованные товары: черновик, архив и удаление удаляют документ,
  поэтому витрина физически не может показать снятый товар.
- Полнотекстовый поиск — анализатор `russian` по названию/описанию/категории/slug/марке
  растворителя, фильтры и сортировка — по `keyword`/числовым полям; `name.keyword` даёт
  стабильный tie-break выдачи. Маппинг создаётся сервисом явно до первого наполнения.
- Правила выдачи (нормализация страницы и размера, разбор сортировки, пустой диапазон цен)
  живут в Application и покрыты юнит-тестами; Infrastructure — только запросы к кластеру.
- Поиск публичный (ADR 0004): JWT в сервисе не подключается, шлюз отдаёт `/search/**` без
  авторизации. Детали сервиса — [src/Services/Search/README.md](../src/Services/Search/README.md).

### 4.3 Аутентификация админ-панели (ADR 0004)

```
Админ-панель -> POST Identity /connect/token
               (grant_type=password, client_id=techodist-admin-panel, scope=...-api)
             -> access-токен (JWT, 15 мин, aud = аудитория нужного сервиса) + refresh-токен (14 дней)
             -> защищённые эндпоинты: сервис/Gateway проверяют подпись, issuer, audience и роль
             -> POST /connect/token (grant_type=refresh_token) — обновление без повторного логина
```

Покупатели не аутентифицируются: витрина каталога публичная, оформление заявки — гостевое
(ADR 0005). Требуют роль `Admin`/`Manager` только административные операции (управление
пользователями в Identity, создание/изменение товаров и категорий в Catalog).

### 4.4 Фронтенд (SPA) и шлюз

```
Браузер (SPA, http://localhost:5173)
  -> API Gateway (http://localhost:5100) — единственный адрес в VITE_API_BASE_URL
     /catalog/api/products          GET         -> Catalog, витрина (только опубликованное)
     /catalog/api/products/admin    GET         -> Catalog, админский срез: черновики и архив
     /catalog/api/products          POST        -> Catalog, создание товара
     /catalog/api/products/{id}     PUT|DELETE  -> Catalog, изменение и удаление черновика
     /catalog/api/products/{id}/publish|archive  -> Catalog, публикация и снятие с продажи
     /catalog/api/categories        POST        -> Catalog, создание категории
     /catalog/api/**                GET         -> Catalog, остальное чтение (префикс снимается трансформом)
     /identity/connect/token -> Identity (grant_type=password | refresh_token)
     /identity/api/**        -> Identity (AuthorizationPolicy techodist-admin-only)
     /basket/api/**          -> Basket (без авторизации: корзина гостя по HttpOnly-cookie)
     /order/api/orders          -> Order, POST (гостевая заявка, rate-limit `order-submit`)
     /order/api/orders/number/* -> Order, GET (статус заявки по номеру из письма, без токена)
     /order/api/orders/...      -> Order, остальные методы (AuthorizationPolicy techodist-admin)
     /search/api/search/products -> Search (публичный полнотекстовый поиск по индексу, без токена)
  -> access-токен на исходе (запас 60 с): клиент продлевает сессию refresh-токеном до запроса
  -> 401 от любого сервиса: store сбрасывает токены, guard уводит на /admin/login
```

- Маршруты витрины: `/catalog` (поиск, фильтры, пагинация), `/catalog/:productId`, `/cart`,
  `/checkout`, `/orders` и `/orders/:number` (статус заявки по номеру из письма).
- Маршруты админки (роли `Admin`/`Manager`, ленивые чанки под `RequireAuth`): `/admin`,
  `/admin/products` (+ `/new`, `/:productId/edit`) и `/admin/categories`.
- Состояние фильтров каталога живёт в query-string, ключи TanStack Query выводятся из тех же
  параметров: кэш переиспользуется, ссылкой на выдачу можно делиться.
- Источник истины — сервер: состав корзины приходит из Basket, заявка создаётся из неё по cookie,
  а мутации пишут ответ в кэш (бейдж корзины и список обновляются без повторного GET).
- Все изменяющие операции каталога закрыты на шлюзе ролью (`techodist-admin`) и повторно проверяются
  сервисом: витрине доступны только чтение и публичные гостевые вызовы.
- CORS шлюза разрешает origin фронтенда (`Cors:AllowedOrigins`); префикс сервиса в пути
  добавляет сам фронтенд — шлюз его снимает перед проксированием. Кэш, инвалидация и продление
  токена описаны в [ADR 0010](adr/0010-frontend-catalog-cart-checkout.md).

### 4.5 Корзина гостя (ADR 0005)

```
Витрина -> Gateway /basket/api/** -> Basket API (без JWT: покупатель не логинится)
             basketId — анонимный GUID; рождается в Basket, уходит гостю HttpOnly-cookie
             (SameSite=Lax, Secure по схеме; JS идентификатор не читает)
          -> Redis: один ключ basket:{basketId} = JSON-состояние корзины, TTL 30 дней
          -> добавление товара: синхронный GET Catalog api/products/{id}
             -> в позиции сохраняется снимок { name, imageUrl, unitPrice }; цена обновляется
                при повторном добавлении, количество прибавляется
```

- Шлюз публикует **гостевой** маршрут: `AuthorizationPolicy` нет, иначе гость не смог бы
  набрать корзину до входа (покупатели не аутентифицируются).
- Cookie — `HttpOnly` и `SameSite=Lax`, а CORS шлюза разрешает `AllowCredentials`:
  фронтенд обращается к API с `withCredentials: true`, иначе браузер cookie не отправит.
- В Redis пишется **отдельная state-модель**, а не агрегат: формат хранения не зависит от
  инкапсуляции домена, а повреждённые данные чинятся при чтении (количество ограничивается
  лимитом позиции). Удаление последней позиции удаляет ключ — пустых корзин не бывает.
- Лимиты: до 50 различных товаров в корзине, до 99 единиц одной позиции.
- При оформлении заявки [4.6](#46-заявки-и-уведомления-adr-0003) `basketId` фиксируется в заказе —
  так корзина связывается с заявкой. Детали сервиса — [src/Services/Basket/README.md](../src/Services/Basket/README.md).

### 4.6 Заявки и уведомления (ADR 0003)

```
Витрина -> Gateway POST /order/api/orders (rate-limit `order-submit`)
             -> Order: basketId из HttpOnly-cookie -> GET Basket api/basket (состав заявки)
                       -> номер TD-ГГГГММДД-00042 (последовательность PostgreSQL)
                       -> заявка + событие OrderSubmitted в outbox — одной транзакцией
             -> RabbitMQ -> Notification (OrderSubmittedConsumer)
                       -> дедупликация по MessageId -> SMTP (MailKit):
                          письмо магазину + подтверждение клиенту
Админка -> Gateway PUT /order/api/orders/{id}/status (роль Admin/Manager)
             -> Order: агрегат проверяет переход -> событие OrderStatusChanged (outbox)
             -> Notification (OrderStatusChangedConsumer): письмо клиенту о новом статусе
```

- Онлайн-оплаты нет: `Pending -> Confirmed -> InProgress -> Completed` (или `Cancelled` на любом
  шаге до терминального статуса) — это работа менеджера с обращением клиента, а не жизненный
  цикл платежа. Повторная установка того же статуса — ошибка: иначе клиент получил бы дубль письма.
- Событие несёт **снимок** заявки (позиции, суммы, контакты, статусы, время), поэтому Notification
  не обращается к Order за данными — обмен только через брокер (ADR 0002).
- У Notification нет своей БД: журнал дедупликации in-memory (retention 24 ч, capacity 10 000),
  а outbox не нужен, так как сервис не пишет в базу. Отметка ставится **после** успешной отправки:
  повторная доставка не дублирует письма, а упавшая отправка не теряется (MassTransit повторит).
- Контракты событий живут в `BuildingBlocks.Messaging.IntegrationEvents` — их используют и
  издатель (Order), и потребитель (Notification), поэтому схема не расходится между сервисами.
- Детали сервисов — [Order](../src/Services/Order/README.md),
  [Notification](../src/Services/Notification/README.md).

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
  В Dev подпись — HS256 ключом из конфигурации (User-Secrets), в прод-контуре — RSA-сертификат
  и JWKS-endpoint; access-токен короткий (15 мин), refresh — одноразовый (14 дней).
- HTTPS через cert-manager. Rate-limiting публичных форм на Gateway.
- Валидация входа (FluentValidation), единый формат ошибок (ProblemDetails).
- Секреты — через K8s Secrets / user-secrets / `.env` (не в git).

## 8. Связанные решения

Детали и обоснования — в [adr/](adr/).

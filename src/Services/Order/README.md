# Order Service

Заявки магазина: гость оформляет заявку из корзины, менеджер ведёт её по статусам.
Онлайн-оплаты нет — заявка уходит письмом магазину, а клиент получает письма от сервиса
Notification по событиям из RabbitMQ (ADR 0003).

## Структура

```
Techodist.Order.Domain          агрегат Order, сущность OrderItem, VO OrderNumber/Money,
                                enum OrderStatus + разрешённые переходы
Techodist.Order.Application     CQRS (MediatR), DTO, Mapster, FluentValidation,
                                абстракции (IOrderRepository, IBasketClient,
                                IOrderNumberGenerator), проекция в события (OrderEventsMapper)
Techodist.Order.Infrastructure  EF Core + PostgreSQL (миграции + таблицы outbox), репозиторий,
                                REST-клиент Basket, номер заявки из последовательности БД,
                                MassTransit (RabbitMQ) с bus outbox, health-check БД
Techodist.Order.Api             контроллер api/orders, чтение cookie корзины, JWT админ-панели,
                                применение миграций при старте, Swagger
tests/Techodist.Order.UnitTests xUnit: домен, обработчики CQRS, валидаторы, маппинг событий,
                                REST-клиент Basket (111 тестов)
```

## Эндпоинты

| Метод | Путь (напрямую) | Через шлюз | Доступ | Описание |
|-------|-----------------|------------|--------|----------|
| POST | `/api/orders` | `/order/api/orders` | гость (cookie корзины, rate-limit) | Оформление заявки |
| GET | `/api/orders/number/{number}` | `/order/api/orders/number/{number}` | гость | Статус по номеру из письма |
| GET | `/api/orders?status=&search=&page=1&pageSize=20` | `/order/api/orders` | роль `Admin`/`Manager` | Постраничный список |
| GET | `/api/orders/{id}` | `/order/api/orders/{id}` | роль `Admin`/`Manager` | Карточка заявки |
| PUT | `/api/orders/{id}/status` | `/order/api/orders/{id}/status` | роль `Admin`/`Manager` | Смена статуса менеджером |
| GET | `/swagger` | — | — | OpenAPI UI |
| GET | `/health/live` | — | — | Живость (проверка БД) |

`basketId` сервис не создаёт: он читает тот же анонимный cookie `techodist_basket`, что и Basket
(ADR 0005). Без cookie (или с битым) оформлять нечего — API отвечает `order.basket.missing`.
Позиции заявки берутся снимком из корзины, поэтому Order не обращается к Catalog.

Ошибки возвращаются как **ProblemDetails (RFC 7807)**: код ошибки лежит в extension `code`,
текст — в `detail`. Ошибки валидации — `ValidationProblemDetails` (400).

| Код | HTTP | Когда |
|-----|------|-------|
| `order.basket.missing` | 400 | Нет cookie корзины (или он битый) |
| `order.basket.empty` | 400 | Корзина пуста или недоступна |
| `order.number.required` | 400 | Номер заявки не передан |
| `order.number.invalid` | 400 | Номер не вида `TD-ГГГГММДД-00001` |
| `order.not_found` | 404 | Заявки с таким Id или номером нет |
| `order.status.already_set` | 409 | Заявка уже в этом статусе |
| `order.status.transition_not_allowed` | 409 | Переход не разрешён воронкой статусов |

## Статусы заявки

```
Pending ──> Confirmed ──> InProgress ──> Completed
   │            │              │
   └────────────┴──────────────┴──────> Cancelled
```

- `Pending` — заявка принята, ждёт менеджера (стартовый статус).
- `Confirmed` — состав и условия подтверждены менеджером.
- `InProgress` — заявка в работе (комплектация, отгрузка).
- `Completed` / `Cancelled` — терминальные статусы, дальше менять нечего.
- Повторная установка текущего статуса запрещена (`order.status.already_set`): иначе клиент получил
  бы два одинаковых письма по одному переходу. Переходы проверяет агрегат (`Order.ChangeStatus`),
  а не контроллер, поэтому правило одно и для API, и для тестов.

## Номер заявки

Номер вида `TD-20261004-00042` — дата оформления плюс сквозной номер дня из последовательности
PostgreSQL (`OrderNumberGenerator`), поэтому два одновременных оформления не получат один номер.
Номер читаемый: клиент называет его менеджеру и по нему же смотрит статус. Внутренний `Id` заявки —
обычный GUID (ссылки в API и админ-панели).

## События (ADR 0003)

События публикуются через **transactional outbox** в БД заявок: `publishEndpoint.Publish` и
`SaveChanges` выполняются в одной транзакции, поэтому «заявка без события» невозможен, а RabbitMQ
может быть недоступен в момент оформления — outbox доставит событие после коммита (`QueryDelay` 5 с).

| Событие | Когда | Кто слушает |
|---------|-------|-------------|
| `OrderSubmittedIntegrationEvent` | После успешного оформления заявки | Notification: письмо магазину + подтверждение клиенту |
| `OrderStatusChangedIntegrationEvent` | После смены статуса менеджером | Notification: письмо клиенту о новом статусе |

События несут **снимок** заявки (номер, позиции, суммы, контакты, статусы, время), поэтому
Notification не обращается к Order за данными. Контракты лежат в
`Techodist.BuildingBlocks.Messaging.IntegrationEvents` — их используют и издатель, и потребитель,
так что схема не расходится между сервисами.

## Конфигурация

| Ключ | Назначение | Dev-значение |
|------|------------|--------------|
| `ConnectionStrings:OrderDb` | БД заявок (миграции + таблицы outbox) | `Host=localhost;Port=5435;Database=techodist_order` |
| `RabbitMq:*` | Брокер для outbox | `localhost:5672`, `techodist` / `techodist_dev_pwd` |
| `Services:Basket:BaseUrl` | Откуда читается корзина гостя | `http://localhost:5103` |
| `Jwt:Issuer` / `Jwt:SigningKey` | Проверка токена админ-панели | issuer Identity, dev-ключ HS256 |
| `Cors:AllowedOrigins` | Origin админ-панели | `http://localhost:5173` |

Аудитория токена — `techodist-order-api` (scope админ-панели в Identity, ADR 0004): токен,
выданный для Catalog, здесь не подойдёт. Публичные маршруты (оформление и статус по номеру) токен
не требуют.

## Миграции EF Core

```powershell
dotnet ef migrations add <Name> `
  --project src/Services/Order/Techodist.Order.Infrastructure `
  --startup-project src/Services/Order/Techodist.Order.Infrastructure `
  --output-dir Persistence/Migrations
```

Миграции применяются автоматически при старте (`OrderDbInitializer`); отдельного сидирования нет —
заявки появляются от гостей, а не из демо-данных. Таблицы outbox создаются той же миграцией, так
что сервис сразу готов публиковать события.

## Запуск

```powershell
# 1) Инфраструктура (PostgreSQL :5435 + RabbitMQ :5672). Требуется Docker Desktop.
docker compose -f deploy/docker-compose/docker-compose.infrastructure.yml up -d order-db rabbitmq mailhog

# 2) Basket — источник позиций заявки (без него оформление вернёт order.basket.empty)
dotnet run --project src/Services/Basket/Techodist.Basket.Api              # http://localhost:5103

# 3) Notification — потребитель событий заявок (письма видны в MailHog :8025)
dotnet run --project src/Services/Notification/Techodist.Notification.Api  # http://localhost:5105

# 4) Сам сервис (миграции применяются при старте)
dotnet run --project src/Services/Order/Techodist.Order.Api                # http://localhost:5104
# Swagger: http://localhost:5104/swagger
```

Проверка «вручную» (через шлюз; cookie корзины приходит от витрины):

```powershell
curl -X POST http://localhost:5100/order/api/orders -H "Content-Type: application/json" `
  -d '{"customerName":"Иван","customerEmail":"ivan@example.com","comment":"Нужен TD60"}'
curl http://localhost:5100/order/api/orders/number/TD-20261004-00001
```

## Тесты

```powershell
dotnet test src/Services/Order/tests/Techodist.Order.UnitTests
```

Домен (переходы статусов, номер, деньги), обработчики CQRS, валидаторы, маппинг событий и клиент
Basket проверяются на фейках (`FakeOrderRepository`, `FakeBasketClient`, `FakeOrderNumberGenerator`,
`FakePublishEndpoint`) — БД и брокер для тестов не нужны. 111 тестов.


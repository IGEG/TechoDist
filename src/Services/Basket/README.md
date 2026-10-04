# Basket Service

Гостевая корзина покупателя: Redis + анонимный HttpOnly-cookie (ADR 0005).
Покупатель не логинится — корзина привязана к анонимному `basketId`, который сервер
кладёт в cookie, а не отдаёт в теле ответа (JavaScript его не читает).

## Структура

```
Techodist.Basket.Domain          агрегат ShoppingBasket, сущность BasketItem, VO Money
Techodist.Basket.Application     CQRS (MediatR), DTO, Mapster, FluentValidation,
                                 абстракции (IBasketRepository, ICatalogProductClient)
Techodist.Basket.Infrastructure  Redis (StackExchangeRedisCache) + state-модели,
                                 REST-клиент Catalog, health-check Redis
Techodist.Basket.Api             контроллеры, cookie/идентификатор корзины, DI, Swagger
tests/Techodist.Basket.UnitTests xUnit: домен, обработчики CQRS, валидаторы, маппер состояния (66 тестов)
```

Имя агрегата — `ShoppingBasket`, а не `Basket`: namespace сервиса `Techodist.Basket`
занял бы имя типа (та же причина, что у `CustomerBasket` в eShop).

## Эндпоинты

| Метод | Путь (напрямую) | Через шлюз | Описание |
|-------|-----------------|------------|----------|
| GET | `/api/basket` | `/basket/api/basket` | Текущая корзина (пустая, если её нет) |
| POST | `/api/basket/items` | `/basket/api/basket/items` | Добавить товар (`{ productId, quantity }`) |
| PUT | `/api/basket/items/{productId}` | `/basket/api/basket/items/{productId}` | Изменить количество (`{ quantity }`) |
| DELETE | `/api/basket/items/{productId}` | `/basket/api/basket/items/{productId}` | Удалить позицию |
| DELETE | `/api/basket` | `/basket/api/basket` | Полная очистка (идемпотентна) |
| GET | `/swagger` | — | OpenAPI UI (только разработка) |
| GET | `/health/live` | — | Живость процесса |

Шлюз снимает префикс `/basket` перед проксированием (ADR 0007), поэтому фронтенд
обращается к `http://localhost:5100/basket/api/basket`.

Примеры ошибок (ProblemDetails):

| Код | HTTP | Когда |
|-----|------|-------|
| `basket.product.not_found` | 404 | Товара нет в каталоге |
| `basket.product.unavailable` | 400 | Товар не опубликован |
| `basket.not_found` | 404 | Корзина не найдена (изменение/удаление позиции) |
| `basket.item.not_found` | 404 | Позиции с таким товаром в корзине нет |
| `basket.item.product_required` | 400 | Не передан товар |
| `basket.item.quantity_out_of_range` | 400 | Количество вне диапазона 1…99 |
| `basket.items.limit_reached` | 409 | Больше 50 различных товаров |

Ошибки валидации приходят как `ValidationProblemDetails` (400).

## Хранение в Redis

- Одна корзина = один JSON-ключ `basket:{basketId}`, TTL 30 дней
  (`Basket:Storage:TtlDays`). Ключ продлевается при каждой записи.
- Сериализуется **не** сам агрегат, а отдельные state-модели
  (`BasketState`/`BasketItemState`): приватные сеттеры и инварианты домена не влияют
  на формат хранения, а повреждённые данные чинятся при чтении (`BasketStateMapper`).
- Удаление последней позиции удаляет ключ целиком — пустых корзин в Redis не бывает.

## Cookie корзины

| Параметр | Значение |
|----------|----------|
| Имя | `techodist_basket` |
| Значения | `HttpOnly`, `SameSite=Lax`, `IsEssential`, `Path=/`, `MaxAge = TTL` |
| `Secure` | по схеме запроса: в dev (http) — выключен, в проде (https) — включён |

`basketId` рождается только в `BasketIdProvider` (первое обращение гостя) и в том же
ответе выдаётся cookie, поэтому идентификатор в cookie и в Redis не расходятся.
Мусор в cookie — не ошибка: гость получает новую корзину.

## Конфигурация

| Ключ | Назначение | Dev-значение |
|------|------------|--------------|
| `ConnectionStrings:Redis` | Адрес Redis | `localhost:6379` |
| `Services:Catalog:BaseUrl` | Откуда берётся снимок товара | `http://localhost:5101` |
| `Basket:Storage:KeyPrefix` | Префикс ключа | `basket:` |
| `Basket:Storage:TtlDays` | TTL корзины, дней | `30` |
| `Cors:AllowedOrigins` | Origin витрины (куки требует `AllowCredentials`) | `http://localhost:5173` |

Снимок товара (название, картинка, цена) берётся синхронным `GET api/products/{id}`
в Catalog: интерактивная операция «в корзину» — сознательный отказ от асинхронности,
покупатель сразу видит актуальную цену. При повторном добавлении известного товара
снимок обновляется, количество прибавляется.

## Запуск

```powershell
# 1) Инфраструктура (Redis :6379). Требуется запущенный Docker Desktop.
docker compose -f deploy/docker-compose/docker-compose.infrastructure.yml up -d redis

# 2) Catalog — источник снимков товара (миграции применяются при старте)
dotnet run --project src/Services/Catalog/Techodist.Catalog.Api   # http://localhost:5101

# 3) Сам сервис
dotnet run --project src/Services/Basket/Techodist.Basket.Api     # http://localhost:5103
```

Проверка «вручную» (через шлюз, куки сохраняются в cookie-jar):

```powershell
# http://localhost:5100 — шлюз; Catalog и Basket должны быть запущены
curl -c cookies.txt -b cookies.txt http://localhost:5100/basket/api/basket
curl -c cookies.txt -b cookies.txt -X POST http://localhost:5100/basket/api/basket/items `
  -H "Content-Type: application/json" -d '{"productId":"<guid товара>","quantity":2}'
```

Cookies работают и напрямую (`http://localhost:5103/api/basket`) — так удобнее отлаживать
в Swagger, но фронтенд ходит только через шлюз.

## Тесты

```powershell
dotnet test src/Services/Basket/tests/Techodist.Basket.UnitTests
```

Инфраструктура в юнит-тестах подменяется фейками (`FakeBasketRepository`,
`FakeCatalogProductClient`), Redis не нужен.

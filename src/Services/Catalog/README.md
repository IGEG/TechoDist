# Catalog Service

Эталонный вертикальный срез сервиса каталога (Clean Architecture + DDD + CQRS).

## Структура

```
Techodist.Catalog.Domain          сущности (Product, Category, ProductImage, ProductSpecification),
                                value objects (Money, Slug), enum ProductStatus
Techodist.Catalog.Application     CQRS (MediatR), DTO, Mapster-маппинг, FluentValidation,
                                абстракции (IProductRepository, ICategoryRepository, ICacheService)
Techodist.Catalog.Infrastructure  EF Core + PostgreSQL (миграции), репозитории, Redis-кэш, health-check
Techodist.Catalog.Api             Minimal hosting, контроллеры, Swagger, сидирование демо-данных
tests/Techodist.Catalog.UnitTests xUnit: value objects, PagedResult, обработчики CQRS (18 тестов)
```

## Эндпоинты

| Метод | Путь | Описание |
|-------|------|----------|
| GET | `/api/products?categoryId=&solventType=&search=&page=1&pageSize=12&sort=price_asc` | Постраничный список (кэш 5 мин) |
| GET | `/api/products/{id}` | Детальная карточка товара |
| POST | `/api/products` | Создание товара (для админки) |
| GET | `/api/categories?onlyPublished=true` | Список категорий |
| POST | `/api/categories` | Создание категории |
| GET | `/swagger` | OpenAPI UI |
| GET | `/health/live` | Проверка живости (включая БД) |

Ошибки возвращаются как **ProblemDetails (RFC 7807)**; ошибки валидации — `ValidationProblemDetails` (400).

## Запуск

```powershell
# 1) Инфраструктура (PostgreSQL :5433 + Redis :6379). Требуется запущенный Docker Desktop.
docker compose -f deploy/docker-compose/docker-compose.infrastructure.yml up -d catalog-db redis

# 2) Запуск сервиса (миграции применяются автоматически при старте)
dotnet run --project src/Services/Catalog/Techodist.Catalog.Api
# Swagger: http://localhost:5101/swagger
```

## Миграции EF Core

```powershell
dotnet ef migrations add <Name> `
  --project src/Services/Catalog/Techodist.Catalog.Infrastructure `
  --startup-project src/Services/Catalog/Techodist.Catalog.Infrastructure `
  --output-dir Persistence/Migrations
```

Design-time фабрика (`CatalogDbContextFactory`) позволяет создавать миграции без запущенной БД.

## Тесты

```powershell
dotnet test src/Services/Catalog/tests/Techodist.Catalog.UnitTests
```

## Модельный ряд (демо-данные)

При первом запуске `CatalogDataSeeder.SeedAsync` сидирует 3 категории и 5 товаров.
Торговая марка оборудования — **Techodist**, сокращённо **TD**; номер модели соответствует объёму бака:

| Модель | Объём | Категория | Цена, ₽ |
|--------|-------|-----------|---------|
| Techodist TD20 | 20 л | Стандартные дистилляторы | 320 000 |
| Techodist TD60 | 60 л | Стандартные дистилляторы | 485 000 |
| Techodist TD120 | 120 л | Стандартные дистилляторы | 720 000 |
| Techodist TDV40 | 40 л | Вакуумные установки | 560 000 |
| Techodist TD-P40 | — | Комплектующие | 38 000 |

Каждый товар получает характеристику «Модель» (по ней позже будет работать фильтр и поиск)
и главное изображение. Константа с названием марки — `CatalogDataSeeder.Brand`.

Пересидировать каталог после правки демо-данных:

```powershell
docker exec techodist-catalog-db psql -U techodist -d techodist_catalog -c 'TRUNCATE "Products","ProductImages","ProductSpecifications","Categories" CASCADE;'
```

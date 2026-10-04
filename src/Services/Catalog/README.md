# Catalog Service

Эталонный вертикальный срез сервиса каталога (Clean Architecture + DDD + CQRS).

## Структура

```
EcoTech.Catalog.Domain          сущности (Product, Category, ProductImage, ProductSpecification),
                                value objects (Money, Slug), enum ProductStatus
EcoTech.Catalog.Application     CQRS (MediatR), DTO, Mapster-маппинг, FluentValidation,
                                абстракции (IProductRepository, ICategoryRepository, ICacheService)
EcoTech.Catalog.Infrastructure  EF Core + PostgreSQL (миграции), репозитории, Redis-кэш, health-check
EcoTech.Catalog.Api             Minimal hosting, контроллеры, Swagger, сидирование демо-данных
tests/EcoTech.Catalog.UnitTests xUnit: value objects, PagedResult, обработчики CQRS (18 тестов)
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
dotnet run --project src/Services/Catalog/EcoTech.Catalog.Api
# Swagger: http://localhost:5101/swagger
```

## Миграции EF Core

```powershell
dotnet ef migrations add <Name> `
  --project src/Services/Catalog/EcoTech.Catalog.Infrastructure `
  --startup-project src/Services/Catalog/EcoTech.Catalog.Infrastructure `
  --output-dir Persistence/Migrations
```

Design-time фабрика (`CatalogDbContextFactory`) позволяет создавать миграции без запущенной БД.

## Тесты

```powershell
dotnet test src/Services/Catalog/tests/EcoTech.Catalog.UnitTests
```

## Данные

При первом запуске сидируются 3 категории и 4 товара (установки регенерации растворителей),
метод `CatalogDataSeeder.SeedAsync`.

# Search Service

Полнотекстовый поиск по товарам витрины на Elasticsearch. Своей БД у сервиса нет:
индекс — денормализованная проекция каталога, которая наполняется событием
`ProductChangedIntegrationEvent` из outbox каталога и доводится до каталога фоновой
реконсиляцией (ADR 0009).

## Структура

```
Techodist.Search.Application     правила выдачи: SearchProductsQuery + обработчик,
                                 ProductSearchFilter (нормализация страницы/размера,
                                 поиск/сортировка, пустой диапазон цен),
                                 ProductSort/ProductSortExtensions, ProductDocument
                                 (read-модель), маппинг Mapster в ProductSearchHitDto,
                                 ProductChangedIndexer, CatalogIndexReconciler
Techodist.Search.Infrastructure  ElasticsearchOptions + ProductIndexMapping (явный маппинг,
                                 анализатор russian), ElasticProductIndex (запросы и upsert),
                                 CatalogProductSource (чтение публичного Catalog API),
                                 IndexReconciliationService (старт + расписание),
                                 health-check Elasticsearch
Techodist.Search.Api             SearchController (GET /api/search/products),
                                 ProductChangedConsumer (MassTransit), DI, Swagger,
                                 /health/live, /health/ready
tests/Techodist.Search.UnitTests xUnit: правила выдачи, индексация, реконсиляция,
                                 health-check, чтение каталога (20 тестов)
```

## Обновление индекса (ADR 0009)

| Путь | Когда работает | Что делает |
|------|----------------|------------|
| Событие | обычный режим: каталог сохранил товар | `ProductChangedConsumer` → `ProductChangedIndexer`: upsert опубликованного товара, delete для черновика/архива/удаления |
| Реконсиляция | старт сервиса и далее каждые 30 мин | `CatalogIndexReconciler`: `EnsureIndexAsync` + upsert всех опубликованных товаров из Catalog API |

- Очереди и обменники создаёт MassTransit (`ConfigureEndpoints`), retry экспоненциальный:
  сбой индексации не теряется — сообщение будет доставлено повторно.
- **Идемпотентность** обеспечена идентификатором документа: `_id` = идентификатор товара,
  поэтому повторная доставка обновляет документ, а не создаёт дубль. Отдельный журнал
  обработанных сообщений (как в Notification) не нужен.
- **Реконсиляция не удаляет** лишние документы: снятые товары исчезают по событию, а полная
  пересборка — операция сопровождения (удалить индекс и дать сервису наполнить его заново).
  Сбой сверки не роняет сервис: индекс продолжает обновляться событиями.

## Индекс и выдача

- Индекс `techodist-products` создаётся сервисом **с явным маппингом**: текст (название,
  описание, имя категории, slug, марка растворителя) — анализатор `russian` (работают
  словоформы: «установка» найдёт «установки»), фильтры и сортировка — `keyword`/числовые
  поля. `name.keyword` — стабильный tie-break выдачи при равном скоре.
- Хранятся только опубликованные товары, но фильтр `isPublished` остаётся в каждом запросе:
  запрос самодостаточен и не покажет снятый товар даже после ручной правки индекса.
- Поиск — `multi_match` с весами: название ×3, краткое описание ×2, slug ×2, имя категории и
  марка растворителя ×1. Сортировка: релевантность (по умолчанию), `price_asc`, `price_desc`,
  `name_asc`, `name_desc` — те же ключи, что у каталога, поэтому ссылки витрины не меняются.
- Общее число совпадений считается точно (`TrackTotalHits`), поэтому пагинация витрины честная.

## API

```
GET /api/search/products?q=&categoryId=&minPrice=&maxPrice=&sort=&page=&pageSize=
```

Публичный эндпоинт: покупатели не аутентифицируются (ADR 0004), JWT в сервисе не подключается.
Через шлюз — `GET http://localhost:5100/search/api/search/products` (маршрут `/search/**`,
префикс снимается). Размер страницы ограничен 50, значение по умолчанию — 12.

## Конфигурация

| Ключ | Назначение | Dev-значение |
|------|------------|--------------|
| `Elasticsearch:Uri` | Адрес кластера | `http://localhost:9200` |
| `Elasticsearch:ProductIndexName` | Имя индекса товаров | `techodist-products` |
| `Elasticsearch:Username` / `:Password` | Учётные данные (X-Pack Security) | пусто |
| `Elasticsearch:RequestTimeoutSeconds` | Таймаут запроса к кластеру | `10` |
| `Elasticsearch:NumberOfShards` / `:NumberOfReplicas` | Топология индекса | `1` / `0` (один узел) |
| `Search:Reindex:Enabled` | Сверка по расписанию (стартовая — всегда) | `true` |
| `Search:Reindex:IntervalMinutes` | Интервал сверки, минут (`0` — только при старте) | `30` |
| `Services:Catalog:BaseUrl` | Каталог для реконсиляции | `http://localhost:5101` |
| `RabbitMq:*` | Брокер событий каталога | `localhost:5672`, `techodist` / `techodist_dev_pwd` |

Адрес брокера должен совпадать с настройкой Catalog: события `ProductChanged` приходят из его outbox.

## Запуск

```powershell
# 1) Инфраструктура (Elasticsearch :9200, Kibana :5601, RabbitMQ :5672). Требуется Docker Desktop.
docker compose -f deploy/docker-compose/docker-compose.infrastructure.yml up -d elasticsearch rabbitmq

# 2) Catalog — издатель событий и источник реконсиляции
dotnet run --project src/Services/Catalog/Techodist.Catalog.Api

# 3) Сам сервис
dotnet run --project src/Services/Search/Techodist.Search.Api   # http://localhost:5106
```

Проверка: опубликовать товар в админ-панели каталога (или изменить его) и запросить
`http://localhost:5106/api/search/products?q=TD60` — документ должен появиться в выдаче.
Проверить содержимое индекса можно в Kibana (http://localhost:5601 →
`GET techodist-products/_search`). Полная пересборка: `DELETE techodist-products` в Kibana —
сервис наполнит индекс при следующем старте или проходе сверки.

## Тесты

```powershell
dotnet test src/Services/Search/tests/Techodist.Search.UnitTests
```

Правила выдачи, индексация по событию и реконсиляция проверяются на фейках
(`FakeProductIndex`, `FakeCatalogProductSource`), чтение каталога — на заглушке HTTP,
health-check — на подменном индексе: Elasticsearch, брокер и Catalog для тестов не нужны.
20 тестов.

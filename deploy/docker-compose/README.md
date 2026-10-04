# Локальная инфраструктура (docker-compose)

Поднимает все инфраструктурные зависимости для разработки. Сами микросервисы
локально обычно запускаются **из IDE/`dotnet run`** и подключаются к этим контейнерам.

## Запуск

```powershell
Copy-Item .env.example .env
docker compose -f deploy/docker-compose/docker-compose.infrastructure.yml up -d
```

Остановить: `... down`. Удалить вместе с данными: `... down -v`.

## Что и на каких портах

| Сервис | Образ | Порт(ы) | Назначение |
|--------|-------|---------|------------|
| catalog-db | postgres:16-alpine | **5433** | БД Catalog (БД `techodist_catalog`) |
| identity-db | postgres:16-alpine | **5434** | БД Identity (БД `techodist_identity`) |
| order-db | postgres:16-alpine | **5435** | БД Order (БД `techodist_order`, включая таблицы outbox) |
| redis | redis:7-alpine | **6379** | Кэш каталога + корзина |
| rabbitmq | rabbitmq:3.13-management-alpine | **5672**, **15672** | Брокер + Management UI (события заявок Order → Notification) |
| elasticsearch | elasticsearch:8.15.0 | **9200** | Поиск товаров (индекс `techodist-products`) |
| kibana | kibana:8.15.0 | **5601** | UI для Elasticsearch |
| mailhog | mailhog/mailhog | **1025**, **8025** | SMTP-ловушка + веб-UI писем (уведомления по заявкам) |

## Полезные адреса

- RabbitMQ Management — http://localhost:15672 (логин/пароль из `.env`, по умолчанию `techodist` / `techodist_dev_pwd`)
- Kibana — http://localhost:5601
- Elasticsearch — http://localhost:9200
- MailHog UI (письма заявок на почту) — http://localhost:8025

Минимальный набор под сервисы заявок (Фаза 6):

```powershell
docker compose -f deploy/docker-compose/docker-compose.infrastructure.yml up -d order-db rabbitmq mailhog
```

Order нужны `order-db` и `rabbitmq`, Notification — `rabbitmq` и `mailhog`; Basket берёт позиции
заявки из `redis` (и обращается к Catalog).

Минимальный набор под поиск (Фаза 7):

```powershell
docker compose -f deploy/docker-compose/docker-compose.infrastructure.yml up -d elasticsearch rabbitmq
```

Search наполняет индекс `techodist-products` в `elasticsearch` событиями `ProductChanged` из
`rabbitmq` и сверкой с публичным Catalog API; Kibana (`:5601`) нужна только для ручной
проверки документов.

## Стек наблюдаемости (ADR 0008)

Метрики, логи и трейсы живут в отдельном compose-файле (`docker-compose.observability.yml`).
Он поднимается независимо от инфраструктуры: у стека своя сеть `observability` и свои порты,
поэтому оба файла спокойно работают одновременно.

```powershell
docker compose -f deploy/docker-compose/docker-compose.observability.yml up -d
```

| Сервис | Образ | Порт(ы) | Назначение |
|--------|-------|---------|------------|
| otel-collector | otel/opentelemetry-collector-contrib:0.111.0 | **4317**, **4318**, 13133, 55679 | приём OTLP и отправка трейсов в Jaeger |
| jaeger | jaegertracing/all-in-one:1.60.0 | **16686** | хранение и UI трейсов (in-memory) |
| prometheus | prom/prometheus:v2.54.1 | **9090** | скрейп `/metrics` сервисов и правила алертов |
| alertmanager | prom/alertmanager:v0.27.0 | **9093** | приём и группировка алертов |
| loki | grafana/loki:3.1.1 | **3100** | хранилище логов (Serilog пишет напрямую) |
| promtail | grafana/promtail:3.1.1 | — | сбор stdout контейнеров Techodist (когда сервисы в Docker) |
| grafana | grafana/grafana:11.2.0 | **3000** | единое окно: дашборды, логи, переход в трейсы |

Логин Grafana — `GRAFANA_ADMIN_USER` / `GRAFANA_ADMIN_PASSWORD` из `.env` (по умолчанию
`admin` / `admin`). Prometheus скрейпит сервисы, запущенные на хосте, по адресам
`host.docker.internal:5100…5106`, поэтому сервисы должны быть подняты (`dotnet run`).
Конфиги стека — `deploy/observability`: описание, дашборды и команды проверки —
[deploy/observability/README.md](../observability/README.md).

## Строки подключения (для `appsettings.Development.json` сервисов)

```
Catalog:    Host=localhost;Port=5433;Database=techodist_catalog;Username=techodist;Password=techodist_dev_pwd
Identity:   Host=localhost;Port=5434;Database=techodist_identity;Username=techodist;Password=techodist_dev_pwd
Order:      Host=localhost;Port=5435;Database=techodist_order;Username=techodist;Password=techodist_dev_pwd
Redis:      localhost:6379 (Catalog — кэш; Basket — корзины `basket:{basketId}`, TTL 30 дней)
RabbitMQ:   amqp://techodist:techodist_dev_pwd@localhost:5672
SMTP:       localhost:1025 (MailHog, без TLS/аутентификации)
Search:     http://localhost:9200 (Elasticsearch, индекс techodist-products; X-Pack Security выключен)
```

> **Замечание.** Elasticsearch доступен в контейнере на `http://localhost:9200`.
> При нехватке памяти замените образ на `opensearchproject/opensearch` (тоже free).

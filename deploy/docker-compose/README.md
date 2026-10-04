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
| order-db | postgres:16-alpine | **5435** | БД Order (БД `techodist_order`) |
| redis | redis:7-alpine | **6379** | Кэш каталога + корзина |
| rabbitmq | rabbitmq:3.13-management-alpine | **5672**, **15672** | Брокер + Management UI |
| elasticsearch | elasticsearch:8.15.0 | **9200** | Поиск товаров (и опц. логи) |
| kibana | kibana:8.15.0 | **5601** | UI для Elasticsearch |
| mailhog | mailhog/mailhog | **1025**, **8025** | SMTP-ловушка + веб-UI писем |

## Полезные адреса

- RabbitMQ Management — http://localhost:15672 (логин/пароль из `.env`, по умолчанию `techodist` / `techodist_dev_pwd`)
- Kibana — http://localhost:5601
- Elasticsearch — http://localhost:9200
- MailHog UI (письма заявок на почту) — http://localhost:8025

## Строки подключения (для `appsettings.Development.json` сервисов)

```
Catalog:    Host=localhost;Port=5433;Database=techodist_catalog;Username=techodist;Password=techodist_dev_pwd
Identity:   Host=localhost;Port=5434;Database=techodist_identity;Username=techodist;Password=techodist_dev_pwd
Order:      Host=localhost;Port=5435;Database=techodist_order;Username=techodist;Password=techodist_dev_pwd
Redis:      localhost:6379
RabbitMQ:   amqp://techodist:techodist_dev_pwd@localhost:5672
SMTP:       localhost:1025 (MailHog, без TLS/аутентификации)
```

> **Замечание.** Elasticsearch доступен в контейнере на `http://localhost:9200`.
> При нехватке памяти замените образ на `opensearchproject/opensearch` (тоже free).

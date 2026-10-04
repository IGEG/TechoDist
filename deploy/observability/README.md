# Наблюдаемость (локальный стек, ADR 0008)

Метрики, логи, трейсы и алерты для разработки: сервисы запускаются на хосте (`dotnet run`),
а телеметрия уезжает в контейнеры из
[`deploy/docker-compose/docker-compose.observability.yml`](../docker-compose/docker-compose.observability.yml).

## Что и где

```
deploy/observability/
├─ otel-collector/otel-collector.yaml        # OTLP-приём → Jaeger
├─ prometheus/prometheus.yml                 # скрейп /metrics сервисов
├─ prometheus/alerts.yml                     # 6 правил алертов
├─ alertmanager/alertmanager.yml             # маршруты, группировка, подавление
├─ loki/loki.yaml                            # хранилище логов (filesystem, retention 7 дней)
├─ promtail/promtail.yaml                    # stdout контейнеров Techodist → Loki
└─ grafana/
   ├─ provisioning/datasources/datasources.yaml   # Prometheus (uid prometheus), Loki (uid loki), Jaeger (uid jaeger)
   ├─ provisioning/dashboards/dashboards.yaml     # папка Techodist, файлы из dashboards/
   └─ dashboards/
      ├─ techodist-services.json              # «Techodist · Сервисы (RED)»
      └─ techodist-business.json              # «Techodist · Бизнес-метрики»
```

## Запуск и адреса

```powershell
# стек наблюдаемости
docker compose -f deploy/docker-compose/docker-compose.observability.yml up -d

# сервисы — как обычно (порты 5100…5106, см. launchSettings.json)
dotnet run --project src/ApiGateway/Techodist.ApiGateway
```

| Инструмент | Адрес | Логин | Назначение |
|------------|-------|-------|------------|
| Grafana | http://localhost:3000 | `admin` / `admin` (`GRAFANA_ADMIN_*` из `.env`) | дашборды, логи Loki, переход в трейс по `TraceId` |
| Prometheus | http://localhost:9090 | — | цели `/metrics`, правила алертов, `POST /-/reload` |
| Jaeger | http://localhost:16686 | — | поиск трейсов по сервису и операции |
| Alertmanager | http://localhost:9093 | — | активные и погашенные алерты |
| Loki | http://localhost:3100/ready | — | готовность хранилища логов |
| Collector | http://localhost:13133/health, http://localhost:55679/debug/tracez | — | здоровье и диагностика OTLP-пайплайна |

Остановка — `... down` (данные томов сохраняются), полная очистка — `... down -v`.

## Как ходит телеметрия

- **Трейсы.** Сервис → OTLP gRPC `localhost:4317` → `otel-collector` → Jaeger (`jaeger:4317`).
  Коллектор батчит спаны, проставляет `deployment.environment=development` и ограничивает память
  (`memory_limiter`).
- **Метрики.** Prometheus раз в 15 с сам забирает `/metrics` сервисов на хосте
  (`host.docker.internal:5100…5106`). Prometheus-экспортёр OpenTelemetry не кладёт в ряд
  идентификатор сервиса (ни `service_name`, ни `target_info`), поэтому имя сервиса проставляет
  сам скрейп (`static_configs.labels.service`) — по этому лейблу фильтруют алерты и дашборды.
- **Логи.** Serilog пишет в консоль и напрямую в Loki (`http://localhost:3100`) с лейблами потока
  `service`, `environment`, `app`; `TraceContextEnricher` добавляет `TraceId`/`SpanId`, а datasource
  Loki в Grafana превращает `TraceId` в ссылку на трейс Jaeger. Promtail собирает только stdout
  контейнеров Techodist — нужен, когда сервисы тоже запускаются в Docker.
- **Алерты.** Prometheus считает правила → Alertmanager (группировка по `alertname`/`service`/`severity`,
  подавление warning-ов при critical по тому же сервису).

## Настройка сервиса

Секция `Observability` в `appsettings.Development.json` (пример — `Techodist.Order.Api`):

```json
"Observability": {
  "OtlpEndpoint": "http://localhost:4317",
  "LokiUri": "http://localhost:3100",
  "TraceSamplingRatio": 1
}
```

| Параметр | По умолчанию | Смысл |
|----------|--------------|-------|
| `OtlpEndpoint` | `http://localhost:4317` | OTLP-эндпоинт коллектора; пусто — трейсы никуда не уходят |
| `LokiUri` | пусто (в dev — `http://localhost:3100`) | адрес Loki; пусто — только консоль |
| `PrometheusEnabled` | `true` | отдавать эндпоинт метрик |
| `PrometheusPath` | `/metrics` | путь эндпоинта метрик |
| `RequestLoggingEnabled` | `true` | одна запись Serilog на HTTP-запрос (`method path → status за ms`) |
| `TraceSamplingRatio` | `1` | доля сэмплируемых трейсов (`ParentBasedSampler` + `TraceIdRatioBasedSampler`) |
| `AdditionalActivitySources`, `AdditionalMeters` | `[]` | дополнительные источники трейсов и меры сверх `Techodist`/`MassTransit` |

Переопределение без правки файлов — переменными окружения: `Observability__LokiUri`,
`Observability__OtlpEndpoint`, `Observability__TraceSamplingRatio` (см. `.env.example`).

## Дашборды

Оба дашборда лежат в папке **Techodist**, у каждого есть переменная `$service` (список
`label_values(up{job!="prometheus"}, service)`), которая фильтрует панели и подставляется
в запросы Prometheus и Loki.

| Дашборд | Файл | Панели |
|---------|------|--------|
| «Techodist · Сервисы (RED)» | `grafana/dashboards/techodist-services.json` | Сервисы доступны, Ошибки 5xx за минуту, Запросы выполняются сейчас (stat и график), Память процессов, Трафик (запросов в секунду), Доля ошибок 5xx, Задержка HTTP p50/p95/p99, CPU процессов .NET, Память .NET (working set), Логи `$service` |
| «Techodist · Бизнес-метрики» | `grafana/dashboards/techodist-business.json` | Оформлено заявок за сутки, Средний чек, Сумма заявок, Письма за сутки, Заявки по каналам связи покупателя, Сумма заявки p50/p95, Смены статусов заявок, Добавления позиций в корзину, Публикации товаров, Отправленные письма по типам, Письма с ошибкой отправки, Ошибки уровня error из Loki |

Панели опираются на семантические конвенции OpenTelemetry для ASP.NET Core
(`http_server_request_duration_seconds_*`, `http_server_active_requests`), runtime-метрики .NET
(`dotnet_process_*`) и бизнес-счётчики `Techodist`
(`techodist_orders_submitted_total`, `techodist_orders_amount_*`,
`techodist_orders_status_changes_total`, `techodist_basket_items_added_total`,
`techodist_catalog_products_published_total`, `techodist_notifications_sent_total`,
`techodist_notifications_failed_total`).

Правки в UI сохраняются в БД Grafana: чтобы вернуть файловую версию, удалите дашборд или
перезапустите Grafana (провижн перечитывает файлы каждые 30 секунд). После правки файлов
provisioning используйте `docker compose ... up -d --force-recreate grafana`.

## Алерты

`deploy/observability/prometheus/alerts.yml` (6 правил) → Alertmanager. Канал доставки по умолчанию
не задан — алерты видны в UI `http://localhost:9093`; email/webhook включается в `alertmanager.yml`
(SMTP-ловушка MailHog уже есть в `docker-compose.infrastructure.yml`).

| Алерт | Условие | `for` | Severity |
|-------|---------|-------|----------|
| `TechodistServiceDown` | `up{job!="prometheus"} == 0` | 2m | critical |
| `TechodistMetricsScrapeSlow` | `scrape_duration_seconds > 7` | 10m | warning |
| `TechodistHighErrorRate` | доля ответов 5xx за 5 минут > 5 % | 5m | warning |
| `TechodistSlowRequests` | p95 длительности запроса за 5 минут > 1.5 с | 10m | warning |
| `TechodistNotificationFailures` | `increase(techodist_notifications_failed_total[15m]) > 3` | 5m | warning |
| `TechodistNoOrders` | `increase(techodist_orders_submitted_total[1h]) == 0` | 1h | info |

## Проверка конфигов

Те же команды выполняет CI-джоба `deploy-configs` (`.github/workflows/ci.yml`):

```bash
# compose-файлы (оба стека)
docker compose -f deploy/docker-compose/docker-compose.infrastructure.yml config -q
docker compose -f deploy/docker-compose/docker-compose.observability.yml config -q

# Prometheus: конфиг и правила алертов
docker run --rm --entrypoint promtool \
  -v "$PWD/deploy/observability/prometheus:/etc/prometheus:ro" \
  prom/prometheus:v2.54.1 check config /etc/prometheus/prometheus.yml
docker run --rm --entrypoint promtool \
  -v "$PWD/deploy/observability/prometheus:/etc/prometheus:ro" \
  prom/prometheus:v2.54.1 check rules /etc/prometheus/alerts.yml

# Alertmanager
docker run --rm --entrypoint amtool \
  -v "$PWD/deploy/observability/alertmanager:/etc/alertmanager:ro" \
  prom/alertmanager:v0.27.0 check-config /etc/alertmanager/alertmanager.yml

# OpenTelemetry Collector
docker run --rm \
  -v "$PWD/deploy/observability/otel-collector:/etc/otelcol-contrib:ro" \
  otel/opentelemetry-collector-contrib:0.111.0 validate --config=/etc/otelcol-contrib/otel-collector.yaml

# Loki и Promtail
docker run --rm \
  -v "$PWD/deploy/observability/loki/loki.yaml:/etc/loki/loki.yaml:ro" \
  grafana/loki:3.1.1 "-config.file=/etc/loki/loki.yaml" "-verify-config"
docker run --rm \
  -v "$PWD/deploy/observability/promtail/promtail.yaml:/etc/promtail/promtail.yaml:ro" \
  grafana/promtail:3.1.1 "-config.file=/etc/promtail/promtail.yaml" "-check-syntax"
```

> В PowerShell флаги Loki/Promtail передаются в кавычках (`"-config.file=…"`): иначе оболочка
> разрывает аргумент и контейнер отвечает `flag provided but not defined: -config`.

После правки конфигов работающего Prometheus хватит перечитать их без перезапуска
(`--web.enable-lifecycle`): `curl -X POST http://localhost:9090/-/reload`.

## Диагностика

| Симптом | Причина и что делать |
|---------|----------------------|
| «Сервисы доступны» пустая, `up=0` | сервис не запущен или не отдаёт `/metrics`; на Linux проверьте `extra_hosts: host.docker.internal:host-gateway` |
| Метрики есть, бизнес-счётчиков нет | сервис не вызывает `TechodistDiagnostics` или его `Meter` не подписан (`AdditionalMeters`) |
| Нет логов в Grafana | не задан `LokiUri` (в appsettings или `Observability__LokiUri`), Loki не поднят, фильтр `{app="techodist"}` не совпал |
| Нет трейсов в Jaeger | пустой `OtlpEndpoint`, коллектор не поднят или `TraceSamplingRatio=0`; смотрите `docker logs techodist-otel-collector` |
| Дашбордов нет в Grafana | ошибка провижна: `docker logs techodist-grafana` (ищите `provisioning`) |
| Алертов нет | не загружены правила: http://localhost:9090/rules; `pending` — правило ждёт `for` |
| Записи в Loki есть, ссылки на трейс нет | `TraceId` отсутствует в записи (лог вне спана) или изменён `matcherRegex` derived field |

## Что дальше (Фаза 10)

- Объектное хранилище для Loki и постоянное хранилище трейсов (Tempo) вместо in-memory Jaeger.
- kube-prometheus-stack в Kubernetes: ServiceMonitor/PodMonitor вместо `static_configs`, тот же
  провижн Grafana в ConfigMap.
- SLO и error budget, метрики брокера (глубина очередей, DLQ, повторы MassTransit).
- Реальный канал доставки алертов (email/webhook) вместо UI Alertmanager.


# Notification Service

Отправка писем по событиям заявок: письмо магазину, подтверждение клиенту и уведомление
о смене статуса. Своей БД у сервиса нет — он слушает RabbitMQ и ходит в SMTP (MailKit),
поэтому transactional outbox ему не нужен (ADR 0003).

## Структура

```
Techodist.Notification.Application     обработчики событий (OrderSubmittedEmailHandler,
                                       OrderStatusChangedEmailHandler), шаблоны писем
                                       (OrderEmailTemplates), абстракции
                                       (IEmailSender, IProcessedMessageStore),
                                       опции (NotificationOptions)
Techodist.Notification.Infrastructure  SMTP-отправитель на MailKit, in-memory журнал
                                       обработанных сообщений (retention + capacity),
                                       health-check SMTP
Techodist.Notification.Api             MassTransit-потребители (OrderSubmittedConsumer,
                                       OrderStatusChangedConsumer), DI, Swagger, /health/live
tests/Techodist.Notification.UnitTests xUnit: шаблоны, обработчики, идемпотентность,
                                       SMTP-отправитель (36 тестов)
```

## Потребители

| Событие | Кто получает письмо | Что внутри |
|---------|---------------------|------------|
| `OrderSubmittedIntegrationEvent` | магазин (`Notifications:StoreEmail`) | номер заявки, контакты, позиции, сумма, комментарий клиента |
| `OrderSubmittedIntegrationEvent` | клиент (если `SendCustomerConfirmation`) | подтверждение: номер заявки, состав, сумма |
| `OrderStatusChangedIntegrationEvent` | клиент | номер заявки, новый статус, комментарий менеджера |

Очереди и обменники создаёт MassTransit автоматически (`ConfigureEndpoints`), retry —
экспоненциальный: 5 попыток, интервал 1 с → 30 с (шаг 2 с). Собственного HTTP API у сервиса нет:
наружу отдаются только `/swagger` и `/health/live` (проверка SMTP), а метрики и трейсы уходят
в OpenTelemetry-стек через OTLP (ADR 0008).

## Идемпотентность (ADR 0003)

- Потребитель передаёт в обработчик `MessageId` брокера; обработчик сначала сверяется с журналом
  обработанных сообщений (`IProcessedMessageStore`).
- Отметка ставится **после** успешной отправки, а исключение не глушится: MassTransit повторит
  доставку, и письмо всё-таки уйдёт. Повторная доставка уже обработанного сообщения писем не
  дублирует.
- Журнал — in-memory с истечением (`Notifications:ProcessedMessages`): `Retention` 24 ч
  (просроченная отметка не считается признаком дубликата) и `Capacity` 10 000 (самые старые
  отметки вытесняются). Хранилище и Redis для журнала не нужны: письма идемпотентны по назначению,
  а повторная отправка при рестарте сервиса не страшна — хуже потерять письмо.

## Шаблоны писем

`OrderEmailTemplates` — чистые функции «событие → письмо»: `StoreNotification`,
`CustomerConfirmation`, `StatusChange`. Шаблоны не знают про SMTP, поэтому проверяются
unit-тестами без брокера и почтового сервера, а отправитель (`SmtpEmailSender`, MailKit)
подменяется фейком.

## Конфигурация

| Ключ | Назначение | Dev-значение |
|------|------------|--------------|
| `Smtp:Host` / `Smtp:Port` | SMTP-сервер | `localhost` / `1025` (MailHog) |
| `Smtp:UseStartTls` | STARTTLS (в dev-ловушке выключен) | `false` |
| `Smtp:Username` / `Smtp:Password` | Учётные данные SMTP | пусто — анонимная отправка |
| `Smtp:FromAddress` / `Smtp:FromName` | Отправитель в письмах | `no-reply@techodist.local` / `Techodist` |
| `Smtp:TimeoutSeconds` | Таймаут соединения с SMTP | `15` |
| `Notifications:StoreEmail` | Ящик магазина для письма о заявке | `orders@techodist.local` |
| `Notifications:StoreName` | Имя магазина в подписи | `Techodist` |
| `Notifications:SendCustomerConfirmation` | Слать ли подтверждение клиенту | `true` |
| `Notifications:ProcessedMessages:Retention` / `:Capacity` | Журнал дедупликации | `24 ч` / `10000` |
| `RabbitMq:*` | Брокер событий | `localhost:5672`, `techodist` / `techodist_dev_pwd` |

Адрес брокера должен совпадать с настройкой Order: события заявок приходят из его outbox.

## Запуск

```powershell
# 1) Инфраструктура (RabbitMQ :5672 + MailHog :1025). Требуется Docker Desktop.
docker compose -f deploy/docker-compose/docker-compose.infrastructure.yml up -d rabbitmq mailhog

# 2) Order — издатель событий заявок (нужен и для «живой» проверки письма)
dotnet run --project src/Services/Order/Techodist.Order.Api

# 3) Сам сервис
dotnet run --project src/Services/Notification/Techodist.Notification.Api   # http://localhost:5105
```

Проверка письма: оформить заявку через витрину/шлюз (`POST http://localhost:5100/order/api/orders`)
и открыть http://localhost:8025 — в MailHog будут письмо магазину и подтверждение клиенту.
Смена статуса в админ-панели добавит туда письмо о новом статусе.

## Тесты

```powershell
dotnet test src/Services/Notification/tests/Techodist.Notification.UnitTests
```

Шаблоны, обработчики и идемпотентность проверяются на фейках (`FakeEmailSender`,
`FakeProcessedMessageStore`, `TestTimeProvider`), отдельно тестируются журнал обработанных
сообщений и health-check SMTP: брокер и SMTP-сервер для тестов не нужны. 36 тестов.


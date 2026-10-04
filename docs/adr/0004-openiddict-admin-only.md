# ADR 0004. OpenIddict, авторизация только для админки

- **Статус:** Accepted
- **Дата:** 2026-04-10

## Контекст

Покупатели оформляют заявку как гости (без регистрации и личного кабинета).
Аутентификация нужна только сотрудникам для доступа к админ-панели. Нужно
бесплатное решение, желательно в экосистеме .NET.

## Решение

- **OpenIddict** + **ASP.NET Core Identity** в сервисе `Identity` (OpenID Connect).
- Роли: `Admin` (полный доступ), `Manager` (обработка заявок). Роли `Customer` нет.
- Регистрации нет: стартовые учётки создаются **сидером** при первом запуске
  (секреты из окружения), далее управление — из админки.
- Токены: access (короткоживущий JWT) + refresh. В dev — симметричный ключ HS256 из
  конфигурации, в прод-контуре — RSA-сертификат и JWKS-endpoint; проверка JWT в Gateway и сервисах.

## Уточнения реализации (Фаза 3)

- **Клиент один** — админ-панель: confidential, `clientId = techodist-admin-panel`,
  client-secret из конфигурации. Разрешены только гранты `password` (вход админа) и
  `refresh_token`. Код-флоу и редиректы не используются (панель — first-party клиент,
  логин/пароль обмениваются на токен напрямую через `POST /connect/token`), поэтому
  authorization/device/introspection/revocation/userinfo/endsession endpoints выключены.
- **Аудитории = scope.** Имена scope совпадают с аудиториями API:
  `techodist-identity-api`, `techodist-catalog-api`, `techodist-order-api`.
  Клиент запрашивает нужные scope, access-токен получает соответствующий `aud`, а сервис
  проверяет **свою** аудиторию (`JwtBearer` → `ValidAudience`). Так токен, выданный для
  Catalog, не подойдёт другому сервису.
- **Подпись и шифрование.** Access-токен — обычный подписанный JWT
  (`DisableAccessTokenEncryption()`), чтобы его могли читать сервисы и Gateway; refresh-токен
  OpenIddict всегда шифрует (`Jwt:EncryptionKey`). Подпись — HS256 (`Jwt:SigningKey`, ≥32 байт,
  проверяется на старте). Для прод-контура: RSA/EC-сертификат + JWKS, проверяющие сервисы
  переходят на `Authority`/`MetadataAddress`, ключ подписи у них исчезает из конфигурации.
- **Claims.** `sub`, `name`, `email`, `role`. Все claims попадают и в access-, и в refresh-токен
  (refresh держит полный набор, чтобы после обновления не терялись роли). JwtBearer настроен
  с `MapInboundClaims = false`, поэтому `[Authorize(Roles = "Admin")]` работает без маппинга.
- **Время жизни.** Access — 15 минут, refresh — 14 дней (одноразовый, «скользящее» обновление).
  Пароли — ASP.NET Core Identity: блокировка учётки на 15 минут после 5 неудачных попыток,
  политика `PasswordPolicy.Default` (≥12 символов, заглавная и строчная буквы, цифра,
  спецсимвол, не совпадает с e-mail) — единый источник правил для домена, FluentValidation
  и настроек Identity.
- **Защита от самоблокировки.** Администратор не может отключить собственную учётку
  (`identity.user.cannot_disable_self`) или снять с себя роль `Admin`
  (`identity.user.cannot_remove_own_admin_role`).
- **Первая учётка** создаётся сидером при старте из переменных
  `Bootstrap__AdminEmail`, `Bootstrap__AdminDisplayName`, `Bootstrap__AdminPassword`
  (в Development — `admin@techodist.local` / `Techodist!Dev2026`).
- **Способ закрыть публичный API.** Административные операции (в Catalog — создание товара и
  категории) помечены `[Authorize(Roles = "Admin,Manager")]`; публичные GET остаются анонимными.

## Последствия

- (+) Полный контроль, всё на .NET, бесплатно (в отличие от платного Duende).
- (+) Гостевая модель упрощает публичную часть.
- (−) При необходимости клиентских кабинетов позже — добавим роль и сценарии.
- Альтернатива (отклонена): Keycloak — мощнее, но отдельный Java-сервис.

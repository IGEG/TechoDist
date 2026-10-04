# ADR 0005. Анонимная корзина: Redis + HttpOnly-cookie

- **Статус:** Accepted
- **Дата:** 2026-04-10

## Контекст

Покупатель не логинится, но нужна полноценная корзина с оформлением заявки.
Нужно связать корзину с гостем и хранить её быстро и с TTL.

## Решение

- Корзина хранится в **Redis**: `basket:{basketId}` → список
  `{ productId, qty, unitPriceSnapshot }`, TTL ~30 дней.
- `basketId` — анонимный GUID, выдаётся **серверным HttpOnly-cookie**
  (`SameSite=Lax`, `Secure`) при первом обращении к Basket.
- Цена/название подтягиваются из **Catalog** (лёгкий sync-вызов gRPC/REST) и
  кэшируются в позиции как снимок на момент добавления.
- При оформлении `basketId` фиксируется в заявке (Order), связывая корзину и заказ.

## Уточнения реализации (Фаза 5)

- Транспорт до Catalog — **REST**, а не gRPC: `GET api/products/{id}` (`CatalogProductClient`).
  gRPC ради одного вызова не стоит отдельного контракта и кодогенерации.
- Идентификатор: cookie `techodist_basket` (HttpOnly, `SameSite=Lax`, `IsEssential`,
  `Path=/`, `MaxAge` = TTL; `Secure` — по схеме запроса, иначе на dev-http браузер
  cookie не сохранит). `basketId` рождается только в `BasketIdProvider`, мусор в cookie = новая корзина.
- Хранение: ключ `basket:{basketId}`, значение — JSON state-моделей (`BasketState`,
  `BasketItemState`), а не сериализованный агрегат: приватные сеттеры домена не диктуют
  формат, а побитые данные чинятся при чтении. TTL ~30 дней (`Basket:Storage:TtlDays`).
- Лимиты: 50 различных товаров (иначе `basket.items.limit_reached`, 409), 99 единиц позиции.
- Шлюз: маршрут `/basket/{**catch-all}` → кластер `basket` (`http://localhost:5103`),
  **без** `AuthorizationPolicy` — корзина гостя, аутентификации нет.
- CORS шлюза с `AllowCredentials` + `withCredentials: true` в axios-клиенте SPA —
  без этого браузер не отправит HttpOnly-cookie на кросс-origin запрос.

## Последствия

- (+) Быстрое чтение/запись, естественный TTL, независимость от устройств браузера.
- (+) HttpOnly-cookie защищает идентификатор от JS-доступа (XSS-риск снижен).
- (−) Корзина не переживает очистку cookie/смену браузера (для гостевой модели ок).
- (-) Требуется CORS + `credentials: include` на фронтенде через Gateway.

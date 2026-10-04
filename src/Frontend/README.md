# Frontend — Techodist (SPA)

React 19 + TypeScript + Vite: витрина магазина (каталог, корзина, оформление заявки) и
админ-панель (вход, CRUD каталога). Наружу SPA ходит только в API Gateway (ADR 0007) —
адресов отдельных сервисов она не знает.

## Стек

| Слой | Технологии |
|------|------------|
| UI | React 19, react-router 7, Tailwind CSS 4 (токены в `src/index.css`), lucide-react |
| Данные | TanStack Query (кэш и мутации), axios (единый клиент — `src/lib/api/client.ts`) |
| Формы | react-hook-form + zod |
| Сессия админки | Zustand + persist в localStorage, продление access-токена по refresh-токену |
| Локализация | i18next, словарь `src/i18n/locales/ru.json` |
| Тесты | vitest + Testing Library + MSW |

## Запуск

```powershell
Copy-Item .env.example .env.local   # адрес шлюза + dev-клиент админ-панели
npm install
npm run dev                         # http://localhost:5173, шлюз — http://localhost:5100
npm run lint                        # ESLint (flat config)
npm run test                        # vitest run
npm run build                       # tsc -b + vite build
```

## Структура

```
src/
├─ components/
│  ├─ layout/          # шапка (навигация + бейдж корзины) и подвал
│  └─ ui/              # примитивы: button, card, alert, input, textarea
├─ features/
│  ├─ auth/            # сессия админ-панели: токены, роли, guard, продление токена
│  ├─ basket/          # гостевая корзина: API, хуки, бейдж, кнопка «В корзину»
│  ├─ catalog/         # каталог витрины и админ-CRUD товаров/категорий
│  └─ orders/          # заявки: оформление и статус по номеру
├─ lib/api/            # axios-клиент, типы DTO сервисов, QueryClient
├─ pages/              # страницы витрины; pages/admin — ленивые чанки админки
├─ i18n/               # словарь ru
└─ test/               # renderWithProviders, MSW-сервер, фикстуры, сборка JWT
```

## Маршруты

| Маршрут | Что делает |
|---------|------------|
| `/` | редирект в каталог |
| `/catalog` | каталог: поиск, фильтр категории и растворителя, пагинация (состояние в query-string) |
| `/catalog/:productId` | карточка товара и кнопка «В корзину» |
| `/cart` | корзина гостя: состав, ± количество (1…99), удаление, очистка, переход к оформлению |
| `/checkout` | оформление заявки: контакты, канал связи, срочность, комментарий |
| `/orders`, `/orders/:number` | поиск и статус заявки по номеру из письма (публично) |
| `/admin/login` | вход сотрудника (OpenIddict password grant через шлюз) |
| `/admin` | карточка сотрудника и разделы панели (роли `Admin`/`Manager`) |
| `/admin/products` | товары: срез со черновиками и архивом, фильтр по статусу, публикация/архив/удаление |
| `/admin/products/new`, `/admin/products/:productId/edit` | создание и правка карточки товара |
| `/admin/categories` | категории: список (включая неопубликованные) и создание |

Админ-разделы закрыты `RequireAuth` (роль `Admin` или `Manager`) и грузятся ленивыми чанками.

## Потоки данных

- **Корзина.** Состояние — на сервере (Redis + анонимный HttpOnly-cookie, ADR 0005). Мутации
  возвращают корзину целиком, хук кладёт её в кэш (`setQueryData`), поэтому бейдж и страница
  обновляются без повторного запроса. Запросы идут с `withCredentials: true`.
- **Заявка.** `POST /order/api/orders` без позиций в теле: Order берёт состав из корзины по cookie.
  После успеха SPA открывает `/orders/{number}`, а кэш корзины инвалидируется (Order очищает её).
- **Админ-CRUD.** Правки инвалидируют и админский, и витринный ключи каталога: Redis-кэш сбрасывает
  сервис Catalog, кэш TanStack Query — `use-admin-catalog.ts`.
- **Токен.** `getAccessToken` — асинхронный хук: если access-токен истекает (запас 60 с), store
  один раз продлевает сессию по refresh-токену; 401 от любого сервиса сбрасывает сессию.
  Подробности — [ADR 0010](../../docs/adr/0010-frontend-catalog-cart-checkout.md).

## Тесты

`npm run test` покрывает API-клиенты (axios + MSW), хуки состояния, страницы витрины и админки:
корзина и её мутации, валидация и отправка заявки, статус заявки по номеру, админский список и
формы товаров/категорий, продление токена. Сервер MSW включается в каждом файле с
`onUnhandledRequest: 'error'`, поэтому незамоканный запрос сразу валит тест.

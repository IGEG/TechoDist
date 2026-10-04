import { HttpResponse, http } from 'msw';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { basketApiUrl, sampleBasket } from '@/test/fixtures';
import { server } from '@/test/server';
import {
  addBasketItem,
  clearBasket,
  fetchBasket,
  removeBasketItem,
  updateBasketItemQuantity,
} from './basket-api';

describe('basket-api', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

  afterEach(() => server.resetHandlers());

  afterAll(() => server.close());

  it('читает корзину гостя с cookie (withCredentials)', async () => {
    let credentials: RequestCredentials | undefined;

    server.use(
      http.get(basketApiUrl, ({ request }) => {
        credentials = request.credentials;

        return HttpResponse.json(sampleBasket());
      }),
    );

    const basket = await fetchBasket();

    // Без cookie корзина каждый раз была бы новой (ADR 0005).
    expect(credentials).toBe('include');
    expect(basket.totalQuantity).toBe(2);
    expect(basket.items[0]?.productName).toBe('Установка регенерации TD-100');
  });

  it('добавляет товар с количеством', async () => {
    let body: Record<string, unknown> = {};

    server.use(
      http.post(`${basketApiUrl}/items`, async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;

        return HttpResponse.json(sampleBasket());
      }),
    );

    await addBasketItem('prod-1', 3);

    expect(body).toEqual({ productId: 'prod-1', quantity: 3 });
  });

  it('меняет количество позиции адресом /items/{productId}', async () => {
    let path = '';
    let body: Record<string, unknown> = {};

    server.use(
      http.put(`${basketApiUrl}/items/prod-1`, async ({ request }) => {
        path = new URL(request.url).pathname;
        body = (await request.json()) as Record<string, unknown>;

        return HttpResponse.json(sampleBasket());
      }),
    );

    await updateBasketItemQuantity('prod-1', 5);

    expect(path).toBe('/basket/api/basket/items/prod-1');
    expect(body).toEqual({ quantity: 5 });
  });

  it('удаляет позицию', async () => {
    let path = '';
    let method = '';

    server.use(
      http.delete(`${basketApiUrl}/items/prod-1`, ({ request }) => {
        path = new URL(request.url).pathname;
        method = request.method;

        return HttpResponse.json(sampleBasket());
      }),
    );

    await removeBasketItem('prod-1');

    expect(method).toBe('DELETE');
    expect(path).toBe('/basket/api/basket/items/prod-1');
  });

  it('очищает корзину целиком', async () => {
    let path = '';

    server.use(
      http.delete(basketApiUrl, ({ request }) => {
        path = new URL(request.url).pathname;

        return HttpResponse.json(sampleBasket({ items: [], totalQuantity: 0, totalAmount: 0 }));
      }),
    );

    const basket = await clearBasket();

    expect(path).toBe('/basket/api/basket');
    expect(basket.items).toHaveLength(0);
  });
});

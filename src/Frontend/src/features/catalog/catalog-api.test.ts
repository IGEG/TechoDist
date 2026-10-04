import { AxiosError } from 'axios';
import { HttpResponse, http } from 'msw';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { useAuthStore } from '@/features/auth/auth-store';
import {
  catalogApiUrl,
  pagedProducts,
  sampleCategories,
  sampleProduct,
  sampleSession,
} from '@/test/fixtures';
import { server } from '@/test/server';
import { fetchCategories, fetchProduct, fetchProducts } from './catalog-api';

describe('catalog-api', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

  afterEach(() => {
    server.resetHandlers();
    useAuthStore.getState().clearSession();
  });

  afterAll(() => server.close());

  it('запрашивает товары через шлюз и передаёт фильтры в query-строку', async () => {
    let receivedParams = new URLSearchParams();

    server.use(
      http.get(`${catalogApiUrl}/products`, ({ request }) => {
        receivedParams = new URL(request.url).searchParams;

        return HttpResponse.json(pagedProducts([sampleProduct()]));
      }),
    );

    const result = await fetchProducts({ page: 2, pageSize: 12, search: 'TD-100' });

    expect(receivedParams.get('page')).toBe('2');
    expect(receivedParams.get('pageSize')).toBe('12');
    expect(receivedParams.get('search')).toBe('TD-100');
    expect(result.items).toHaveLength(1);
    expect(result.items[0]?.name).toBe('Установка регенерации TD-100');
  });

  it('ставит page=1 по умолчанию, если фильтры не заданы', async () => {
    let receivedParams = new URLSearchParams();

    server.use(
      http.get(`${catalogApiUrl}/products`, ({ request }) => {
        receivedParams = new URL(request.url).searchParams;

        return HttpResponse.json(pagedProducts([]));
      }),
    );

    await fetchProducts();

    expect(receivedParams.get('page')).toBe('1');
    expect(receivedParams.has('search')).toBe(false);
  });

  it('подставляет access-токен администратора в Authorization', async () => {
    let authorization: string | null = null;

    useAuthStore.getState().setSession(sampleSession(['Admin']));

    server.use(
      http.get(`${catalogApiUrl}/categories`, ({ request }) => {
        authorization = request.headers.get('authorization');

        return HttpResponse.json(sampleCategories);
      }),
    );

    const categories = await fetchCategories();

    expect(authorization).toBe('Bearer access-token');
    expect(categories.map((category) => category.slug)).toEqual(['td', 'parts']);
  });

  it('пробрасывает 404 карточки товара как ошибку axios', async () => {
    server.use(
      http.get(`${catalogApiUrl}/products/missing`, () =>
        HttpResponse.json({ status: 404, title: 'Товар не найден' }, { status: 404 }),
      ),
    );

    await expect(fetchProduct('missing')).rejects.toBeInstanceOf(AxiosError);
  });

  it('сбрасывает сессию при 401 — истёкший токен не остаётся в store', async () => {
    useAuthStore.getState().setSession(sampleSession(['Admin']));

    server.use(
      http.get(`${catalogApiUrl}/products`, () =>
        HttpResponse.json({ status: 401, title: 'Unauthorized' }, { status: 401 }),
      ),
    );

    await expect(fetchProducts()).rejects.toBeInstanceOf(AxiosError);

    expect(useAuthStore.getState().session).toBeNull();
  });
});

import { HttpResponse, http } from 'msw';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { catalogApiUrl, pagedProducts, sampleCategories, sampleProduct } from '@/test/fixtures';
import { server } from '@/test/server';
import {
  archiveProduct,
  createCategory,
  createProduct,
  deleteProduct,
  fetchAdminCategories,
  fetchAdminProducts,
  publishProduct,
  updateProduct,
} from './admin-catalog-api';

describe('admin-catalog-api', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

  afterEach(() => server.resetHandlers());

  afterAll(() => server.close());

  it('читает админский список с фильтром по статусу', async () => {
    let params = new URLSearchParams();

    server.use(
      http.get(`${catalogApiUrl}/products/admin`, ({ request }) => {
        params = new URL(request.url).searchParams;

        return HttpResponse.json(pagedProducts([sampleProduct({ status: 'Draft' })]));
      }),
    );

    const page = await fetchAdminProducts({ status: 'Draft', search: 'TD-100', page: 2, pageSize: 20 });

    expect(params.get('status')).toBe('Draft');
    expect(params.get('search')).toBe('TD-100');
    expect(params.get('page')).toBe('2');
    expect(page.items[0]?.status).toBe('Draft');
  });

  it('запрашивает категории вместе с неопубликованными', async () => {
    let params = new URLSearchParams();

    server.use(
      http.get(`${catalogApiUrl}/categories`, ({ request }) => {
        params = new URL(request.url).searchParams;

        return HttpResponse.json(sampleCategories);
      }),
    );

    const categories = await fetchAdminCategories();

    expect(params.get('onlyPublished')).toBe('false');
    expect(categories).toHaveLength(2);
  });

  it('создаёт товар и возвращает идентификатор', async () => {
    let body: Record<string, unknown> = {};

    server.use(
      http.post(`${catalogApiUrl}/products`, async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;

        return HttpResponse.json('prod-new', { status: 201 });
      }),
    );

    const id = await createProduct({
      name: 'Установка TDV40',
      categoryId: 'cat-1',
      price: 1250,
      slug: null,
      volumeLiters: 40,
    });

    expect(id).toBe('prod-new');
    expect(body).toEqual({
      name: 'Установка TDV40',
      categoryId: 'cat-1',
      price: 1250,
      slug: null,
      volumeLiters: 40,
    });
  });

  it('изменяет товар через PUT', async () => {
    let path = '';
    let body: Record<string, unknown> = {};

    server.use(
      http.put(`${catalogApiUrl}/products/prod-1`, async ({ request }) => {
        path = new URL(request.url).pathname;
        body = (await request.json()) as Record<string, unknown>;

        return new HttpResponse(null, { status: 204 });
      }),
    );

    await updateProduct('prod-1', { name: 'Установка TD-100 v2', categoryId: 'cat-1', price: 1500 });

    expect(path).toBe('/catalog/api/products/prod-1');
    expect(body.name).toBe('Установка TD-100 v2');
    expect(body.price).toBe(1500);
  });

  it('публикует и снимает товар с продажи', async () => {
    const paths: string[] = [];

    server.use(
      http.post(`${catalogApiUrl}/products/prod-1/publish`, ({ request }) => {
        paths.push(new URL(request.url).pathname);

        return HttpResponse.json('prod-1');
      }),
      http.post(`${catalogApiUrl}/products/prod-1/archive`, ({ request }) => {
        paths.push(new URL(request.url).pathname);

        return HttpResponse.json('prod-1');
      }),
    );

    await publishProduct('prod-1');
    await archiveProduct('prod-1');

    expect(paths).toEqual([
      '/catalog/api/products/prod-1/publish',
      '/catalog/api/products/prod-1/archive',
    ]);
  });

  it('удаляет черновик', async () => {
    let path = '';
    let method = '';

    server.use(
      http.delete(`${catalogApiUrl}/products/prod-1`, ({ request }) => {
        path = new URL(request.url).pathname;
        method = request.method;

        return new HttpResponse(null, { status: 204 });
      }),
    );

    await deleteProduct('prod-1');

    expect(method).toBe('DELETE');
    expect(path).toBe('/catalog/api/products/prod-1');
  });

  it('создаёт категорию', async () => {
    let body: Record<string, unknown> = {};

    server.use(
      http.post(`${catalogApiUrl}/categories`, async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;

        return HttpResponse.json('cat-new', { status: 201 });
      }),
    );

    const id = await createCategory({ name: 'Вакуумные установки', sortOrder: 3 });

    expect(id).toBe('cat-new');
    expect(body).toEqual({ name: 'Вакуумные установки', sortOrder: 3 });
  });
});

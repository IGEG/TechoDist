import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { AdminProductsPage } from '@/pages/admin/AdminProductsPage';
import { catalogApiUrl, pagedProducts, sampleProduct } from '@/test/fixtures';
import { renderWithProviders } from '@/test/render';
import { server } from '@/test/server';

const adminProductsHandler = (items = [sampleProduct({ status: 'Draft' })]) =>
  http.get(`${catalogApiUrl}/products/admin`, () => HttpResponse.json(pagedProducts(items)));

const categoriesHandler = () => http.get(`${catalogApiUrl}/categories`, () => HttpResponse.json([]));

describe('AdminProductsPage', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

  afterEach(() => server.resetHandlers());

  afterAll(() => server.close());

  it('показывает черновик с действиями публикации и удаления', async () => {
    server.use(adminProductsHandler(), categoriesHandler());

    renderWithProviders(<AdminProductsPage />);

    expect(
      await screen.findByRole('link', { name: 'Установка регенерации TD-100' }),
    ).toBeInTheDocument();

    // Статус читаем в таблице: то же слово есть в фильтре статусов выше.
    const row = within(screen.getByRole('table'));
    expect(row.getByText('Черновик')).toBeInTheDocument();
    expect(row.getByRole('button', { name: 'Опубликовать' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Снять с продажи' })).not.toBeInTheDocument();
    expect(
      row.getByRole('button', { name: 'Удалить черновик: Установка регенерации TD-100' }),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Создать товар' })).toHaveAttribute(
      'href',
      '/admin/products/new',
    );
  });

  it('для опубликованного товара предлагает архив вместо удаления', async () => {
    server.use(adminProductsHandler([sampleProduct({ status: 'Published' })]), categoriesHandler());

    renderWithProviders(<AdminProductsPage />);

    await screen.findByRole('link', { name: 'Установка регенерации TD-100' });

    const row = within(screen.getByRole('table'));
    expect(row.getByText('Опубликован')).toBeInTheDocument();
    expect(row.getByRole('button', { name: 'Снять с продажи' })).toBeInTheDocument();
    expect(row.queryByRole('button', { name: /Удалить черновик/ })).not.toBeInTheDocument();
  });

  it('публикует товар', async () => {
    const user = userEvent.setup();
    let path = '';

    server.use(
      adminProductsHandler(),
      categoriesHandler(),
      http.post(`${catalogApiUrl}/products/prod-1/publish`, ({ request }) => {
        path = new URL(request.url).pathname;

        return HttpResponse.json('prod-1');
      }),
    );

    renderWithProviders(<AdminProductsPage />);

    await user.click(await screen.findByRole('button', { name: 'Опубликовать' }));

    await waitFor(() => expect(path).toBe('/catalog/api/products/prod-1/publish'));
  });

  it('удаляет черновик', async () => {
    const user = userEvent.setup();
    let method = '';

    server.use(
      adminProductsHandler(),
      categoriesHandler(),
      http.delete(`${catalogApiUrl}/products/prod-1`, ({ request }) => {
        method = request.method;

        return new HttpResponse(null, { status: 204 });
      }),
    );

    renderWithProviders(<AdminProductsPage />);

    await user.click(
      await screen.findByRole('button', { name: 'Удалить черновик: Установка регенерации TD-100' }),
    );

    await waitFor(() => expect(method).toBe('DELETE'));
  });

  it('передаёт выбранный статус в фильтр запроса', async () => {
    const user = userEvent.setup();
    let status = '';

    server.use(
      http.get(`${catalogApiUrl}/products/admin`, ({ request }) => {
        status = new URL(request.url).searchParams.get('status') ?? '';

        return HttpResponse.json(pagedProducts([sampleProduct({ status: 'Archived' })]));
      }),
      categoriesHandler(),
    );

    renderWithProviders(<AdminProductsPage />);

    await screen.findByText('Установка регенерации TD-100');
    await user.selectOptions(screen.getByLabelText('Статус'), 'Archived');
    await user.click(screen.getByRole('button', { name: 'Найти' }));

    await waitFor(() => expect(status).toBe('Archived'));
    expect(within(screen.getByRole('table')).getByText('В архиве')).toBeInTheDocument();
  });

  it('показывает ошибку, если список товаров недоступен', async () => {
    server.use(
      http.get(`${catalogApiUrl}/products/admin`, () =>
        HttpResponse.json({ status: 500, title: 'Server Error' }, { status: 500 }),
      ),
      categoriesHandler(),
    );

    renderWithProviders(<AdminProductsPage />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось загрузить товары');
  });
});

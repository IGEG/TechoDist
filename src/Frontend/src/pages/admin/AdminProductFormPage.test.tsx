import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { Route, Routes } from 'react-router';
import { AdminProductFormPage } from '@/pages/admin/AdminProductFormPage';
import { catalogApiUrl, sampleCategories, sampleProductDetails } from '@/test/fixtures';
import { renderWithProviders } from '@/test/render';
import { server } from '@/test/server';

const categoriesHandler = () =>
  http.get(`${catalogApiUrl}/categories`, () => HttpResponse.json(sampleCategories));

/** Админка открывает форму по /new и /:productId/edit. */
function renderForm(path = '/admin/products/new') {
  return renderWithProviders(
    <Routes>
      <Route path="admin/products/new" element={<AdminProductFormPage />} />
      <Route path="admin/products/:productId/edit" element={<AdminProductFormPage />} />
      <Route path="admin/products" element={<p>Список товаров</p>} />
    </Routes>,
    { route: path },
  );
}

describe('AdminProductFormPage', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

  afterEach(() => server.resetHandlers());

  afterAll(() => server.close());

  it('проверяет обязательные поля и категорию', async () => {
    const user = userEvent.setup();
    let posts = 0;

    server.use(
      categoriesHandler(),
      http.post(`${catalogApiUrl}/products`, () => {
        posts += 1;

        return HttpResponse.json('prod-new', { status: 201 });
      }),
    );

    renderForm();

    await screen.findByLabelText('Категория');
    await user.click(screen.getByRole('button', { name: 'Сохранить' }));

    expect(await screen.findByText('Укажите название товара')).toBeInTheDocument();
    expect(screen.getByText('Выберите категорию товара')).toBeInTheDocument();
    expect(posts).toBe(0);
  });

  it('создаёт товар и возвращает к списку', async () => {
    const user = userEvent.setup();
    let body: Record<string, unknown> = {};

    server.use(
      categoriesHandler(),
      http.post(`${catalogApiUrl}/products`, async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;

        return HttpResponse.json('prod-new', { status: 201 });
      }),
    );

    renderForm();

    await user.type(await screen.findByLabelText('Название'), 'Установка TDV40');
    await user.selectOptions(screen.getByLabelText('Категория'), 'cat-1');
    await user.clear(screen.getByLabelText('Цена, ₽'));
    await user.type(screen.getByLabelText('Цена, ₽'), '1250');
    await user.type(screen.getByLabelText('Объём, л'), '40');
    await user.type(screen.getByLabelText('Slug'), 'tdv-40');
    await user.click(screen.getByRole('button', { name: 'Сохранить' }));

    expect(await screen.findByText('Список товаров')).toBeInTheDocument();
    expect(body).toEqual({
      name: 'Установка TDV40',
      categoryId: 'cat-1',
      price: 1250,
      slug: 'tdv-40',
      shortDescription: null,
      description: null,
      solventType: null,
      volumeLiters: 40,
    });
  });

  it('заполняет форму карточкой товара при правке', async () => {
    server.use(
      categoriesHandler(),
      http.get(`${catalogApiUrl}/products/prod-1`, () => HttpResponse.json(sampleProductDetails())),
    );

    renderForm('/admin/products/prod-1/edit');

    await waitFor(() =>
      expect(screen.getByLabelText('Название')).toHaveValue('Установка регенерации TD-100'),
    );

    expect(screen.getByLabelText('Категория')).toHaveValue('cat-1');
    expect(screen.getByLabelText('Цена, ₽')).toHaveValue('1250');
    expect(screen.getByLabelText('Объём, л')).toHaveValue('100');
    expect(screen.getByLabelText('Slug')).toHaveValue('td-100');
    expect(screen.getByRole('heading', { name: 'Редактирование товара' })).toBeInTheDocument();
  });

  it('изменяет товар через PUT', async () => {
    const user = userEvent.setup();
    let path = '';
    let body: Record<string, unknown> = {};

    server.use(
      categoriesHandler(),
      http.get(`${catalogApiUrl}/products/prod-1`, () => HttpResponse.json(sampleProductDetails())),
      http.put(`${catalogApiUrl}/products/prod-1`, async ({ request }) => {
        path = new URL(request.url).pathname;
        body = (await request.json()) as Record<string, unknown>;

        return new HttpResponse(null, { status: 204 });
      }),
    );

    renderForm('/admin/products/prod-1/edit');

    await waitFor(() => expect(screen.getByLabelText('Цена, ₽')).toHaveValue('1250'));

    await user.clear(screen.getByLabelText('Цена, ₽'));
    await user.type(screen.getByLabelText('Цена, ₽'), '1500');
    await user.click(screen.getByRole('button', { name: 'Сохранить' }));

    expect(await screen.findByText('Список товаров')).toBeInTheDocument();
    expect(path).toBe('/catalog/api/products/prod-1');
    expect(body.price).toBe(1500);
    expect(body.name).toBe('Установка регенерации TD-100');
  });

  it('подсказывает создать категорию, если их ещё нет', async () => {
    server.use(http.get(`${catalogApiUrl}/categories`, () => HttpResponse.json([])));

    renderForm();

    expect(
      await screen.findByText('В каталоге ещё нет категорий — создайте первую:'),
    ).toBeInTheDocument();
  });
});

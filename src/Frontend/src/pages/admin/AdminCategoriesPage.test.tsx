import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { AdminCategoriesPage } from '@/pages/admin/AdminCategoriesPage';
import { catalogApiUrl, sampleCategories } from '@/test/fixtures';
import { renderWithProviders } from '@/test/render';
import { server } from '@/test/server';

const categoriesHandler = () =>
  http.get(`${catalogApiUrl}/categories`, ({ request }) => {
    const onlyPublished = new URL(request.url).searchParams.get('onlyPublished');

    // Админка просит весь список, витрина — только опубликованные.
    return HttpResponse.json(
      onlyPublished === 'false' ? sampleCategories : sampleCategories.filter((c) => c.isPublished),
    );
  });

describe('AdminCategoriesPage', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

  afterEach(() => server.resetHandlers());

  afterAll(() => server.close());

  it('показывает категории каталога', async () => {
    server.use(categoriesHandler());

    renderWithProviders(<AdminCategoriesPage />);

    expect(await screen.findByText('Установки TD-серии')).toBeInTheDocument();
    expect(screen.getByText('Комплектующие')).toBeInTheDocument();
    expect(screen.getByText('td')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Новая категория' })).toBeInTheDocument();
  });

  it('создаёт категорию и очищает форму', async () => {
    const user = userEvent.setup();
    let body: Record<string, unknown> = {};

    server.use(
      categoriesHandler(),
      http.post(`${catalogApiUrl}/categories`, async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;

        return HttpResponse.json('cat-new', { status: 201 });
      }),
    );

    renderWithProviders(<AdminCategoriesPage />);

    await screen.findByText('Установки TD-серии');

    await user.type(screen.getByLabelText('Название'), 'Вакуумные установки');
    await user.click(screen.getByRole('button', { name: 'Создать категорию' }));

    expect(await screen.findByText('Категория создана')).toBeInTheDocument();
    expect(body).toEqual({
      name: 'Вакуумные установки',
      slug: null,
      sortOrder: 0,
      description: null,
    });
    expect(screen.getByLabelText('Название')).toHaveValue('');
  });

  it('проверяет обязательное название', async () => {
    const user = userEvent.setup();
    let posts = 0;

    server.use(
      categoriesHandler(),
      http.post(`${catalogApiUrl}/categories`, () => {
        posts += 1;

        return HttpResponse.json('cat-new', { status: 201 });
      }),
    );

    renderWithProviders(<AdminCategoriesPage />);

    await screen.findByText('Установки TD-серии');
    await user.click(screen.getByRole('button', { name: 'Создать категорию' }));

    expect(await screen.findByText('Укажите название категории')).toBeInTheDocument();
    expect(posts).toBe(0);
  });

  it('показывает ошибку каталога (дубль названия)', async () => {
    const user = userEvent.setup();

    server.use(
      categoriesHandler(),
      http.post(`${catalogApiUrl}/categories`, () =>
        HttpResponse.json(
          {
            status: 409,
            title: 'Конфликт',
            detail: "Категория 'Комплектующие' уже существует.",
          },
          { status: 409 },
        ),
      ),
    );

    renderWithProviders(<AdminCategoriesPage />);

    await screen.findByText('Установки TD-серии');

    await user.type(screen.getByLabelText('Название'), 'Комплектующие');
    await user.click(screen.getByRole('button', { name: 'Создать категорию' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      "Категория 'Комплектующие' уже существует.",
    );
  });
});

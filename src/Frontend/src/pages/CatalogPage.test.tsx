import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { CatalogPage } from '@/pages/CatalogPage';
import {
  catalogApiUrl,
  categoriesHandler,
  pagedProducts,
  productsHandler,
  sampleProduct,
} from '@/test/fixtures';
import { renderWithProviders } from '@/test/render';
import { server } from '@/test/server';

describe('CatalogPage', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

  afterEach(() => server.resetHandlers());

  afterAll(() => server.close());

  it('показывает товары каталога и ссылку на карточку', async () => {
    server.use(productsHandler(), categoriesHandler());

    renderWithProviders(<CatalogPage />);

    expect(
      await screen.findByRole('heading', { name: 'Установка регенерации TD-100' }),
    ).toBeInTheDocument();
    expect(screen.getByText('1 250 ₽')).toBeInTheDocument();
    expect(screen.getByText('Хлорсодержащие')).toBeInTheDocument();
    expect(screen.getByText('100 л')).toBeInTheDocument();
    expect(screen.getByText('Найдено товаров: 1')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Подробнее' })).toHaveAttribute(
      'href',
      '/catalog/prod-1',
    );
  });

  it('показывает пустое состояние, если товаров нет', async () => {
    server.use(productsHandler([]), categoriesHandler());

    renderWithProviders(<CatalogPage />);

    expect(
      await screen.findByText('Ничего не найдено. Измените условия поиска.'),
    ).toBeInTheDocument();
  });

  it('передаёт поисковый запрос в API при отправке формы', async () => {
    const user = userEvent.setup();
    const searchValues: string[] = [];

    server.use(
      http.get(`${catalogApiUrl}/products`, ({ request }) => {
        const search = new URL(request.url).searchParams.get('search') ?? '';
        searchValues.push(search);

        return HttpResponse.json(
          pagedProducts(
            search
              ? [sampleProduct({ id: 'prod-2', name: 'Установка регенерации TD-200' })]
              : [sampleProduct()],
          ),
        );
      }),
      categoriesHandler(),
    );

    renderWithProviders(<CatalogPage />);

    await screen.findByText('Установка регенерации TD-100');

    await user.type(screen.getByLabelText('Поиск'), 'TD-200');
    await user.click(screen.getByRole('button', { name: 'Найти' }));

    expect(await screen.findByText('Установка регенерации TD-200')).toBeInTheDocument();
    expect(searchValues).toContain('TD-200');
  });

  it('переключает страницы каталога', async () => {
    const user = userEvent.setup();

    server.use(
      http.get(`${catalogApiUrl}/products`, ({ request }) => {
        const page = Number(new URL(request.url).searchParams.get('page') ?? '1');

        return HttpResponse.json({
          ...pagedProducts([sampleProduct({ name: page === 2 ? 'Страница два' : 'Страница один' })]),
          page,
          totalCount: 2,
          totalPages: 2,
          hasNext: page === 1,
          hasPrevious: page === 2,
        });
      }),
      categoriesHandler(),
    );

    renderWithProviders(<CatalogPage />);

    expect(await screen.findByText('Страница один')).toBeInTheDocument();
    expect(screen.getByText('Страница 1 из 2')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Вперёд' }));

    expect(await screen.findByText('Страница два')).toBeInTheDocument();
    expect(screen.getByText('Страница 2 из 2')).toBeInTheDocument();
  });

  it('показывает ошибку, если каталог недоступен', async () => {
    server.use(
      http.get(`${catalogApiUrl}/products`, () =>
        HttpResponse.json({ status: 503, title: 'Service Unavailable' }, { status: 503 }),
      ),
      categoriesHandler(),
    );

    renderWithProviders(<CatalogPage />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось загрузить каталог');
  });
});

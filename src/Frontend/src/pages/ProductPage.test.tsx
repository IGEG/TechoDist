import { screen } from '@testing-library/react';
import { HttpResponse, http } from 'msw';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { Route, Routes } from 'react-router';
import { ProductPage } from '@/pages/ProductPage';
import { catalogApiUrl, sampleProductDetails } from '@/test/fixtures';
import { renderWithProviders } from '@/test/render';
import { server } from '@/test/server';

/** Страница товара читает id из URL, поэтому путь полностью задаёт сценарий. */
function renderProductPage(path = '/catalog/prod-1') {
  return renderWithProviders(
    <Routes>
      <Route path="catalog/:productId" element={<ProductPage />} />
    </Routes>,
    { route: path },
  );
}

describe('ProductPage', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

  afterEach(() => server.resetHandlers());

  afterAll(() => server.close());

  it('показывает описание, цену и характеристики товара', async () => {
    server.use(
      http.get(`${catalogApiUrl}/products/prod-1`, () => HttpResponse.json(sampleProductDetails())),
    );

    renderProductPage();

    expect(
      await screen.findByRole('heading', { name: 'Установка регенерации TD-100' }),
    ).toBeInTheDocument();
    expect(screen.getByText('1 250 ₽')).toBeInTheDocument();
    expect(screen.getByText('Хлорсодержащие')).toBeInTheDocument();
    expect(screen.getByText('Компактная установка для участка мойки.')).toBeInTheDocument();
    expect(screen.getByText('Напряжение')).toBeInTheDocument();
    expect(screen.getByText('380 В')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'К каталогу' })).toHaveAttribute('href', '/catalog');
  });

  it('показывает «Цена по запросу», если цена не задана', async () => {
    server.use(
      http.get(`${catalogApiUrl}/products/prod-1`, () =>
        HttpResponse.json(sampleProductDetails({ price: 0, images: [], specifications: [] })),
      ),
    );

    renderProductPage();

    expect(await screen.findByText('Цена по запросу')).toBeInTheDocument();
  });

  it('показывает ошибку для неизвестного товара', async () => {
    server.use(
      http.get(`${catalogApiUrl}/products/missing`, () =>
        HttpResponse.json({ status: 404, title: 'Товар не найден' }, { status: 404 }),
      ),
    );

    renderProductPage('/catalog/missing');

    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось загрузить товар');
  });
});

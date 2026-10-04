import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { BasketPage } from '@/pages/BasketPage';
import { basketApiUrl, basketHandler, emptyBasket, sampleBasket } from '@/test/fixtures';
import { renderWithProviders } from '@/test/render';
import { server } from '@/test/server';

describe('BasketPage', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

  afterEach(() => server.resetHandlers());

  afterAll(() => server.close());

  it('показывает позиции, сумму и переход к оформлению', async () => {
    server.use(basketHandler());

    renderWithProviders(<BasketPage />);

    expect(await screen.findByText('Установка регенерации TD-100')).toBeInTheDocument();
    expect(screen.getByText('Товаров: 2')).toBeInTheDocument();
    // Testing Library нормализует NBSP, поэтому ожидание пишется с обычными пробелами.
    expect(screen.getByText('Итого: 2 500 ₽')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Оформить заявку' })).toHaveAttribute(
      'href',
      '/checkout',
    );
    // Снимок цены берётся из корзины, а не из каталога (ADR 0005).
    expect(screen.getByText('1 250 ₽')).toBeInTheDocument();
  });

  it('увеличивает количество позиции', async () => {
    const user = userEvent.setup();
    let body: Record<string, unknown> = {};

    server.use(
      basketHandler(),
      http.put(`${basketApiUrl}/items/prod-1`, async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;

        return HttpResponse.json(sampleBasket({ totalQuantity: 3, totalAmount: 3750 }));
      }),
    );

    renderWithProviders(<BasketPage />);

    await screen.findByText('Установка регенерации TD-100');
    await user.click(screen.getByRole('button', { name: 'Увеличить количество' }));

    await waitFor(() => expect(body).toEqual({ quantity: 3 }));
    expect(await screen.findByText('Итого: 3 750 ₽')).toBeInTheDocument();
  });

  it('удаляет позицию и показывает пустую корзину', async () => {
    const user = userEvent.setup();

    server.use(
      basketHandler(),
      http.delete(`${basketApiUrl}/items/prod-1`, () => HttpResponse.json(emptyBasket)),
    );

    renderWithProviders(<BasketPage />);

    await screen.findByText('Установка регенерации TD-100');
    await user.click(
      screen.getByRole('button', { name: 'Удалить: Установка регенерации TD-100' }),
    );

    expect(
      await screen.findByText('Корзина пуста. Добавьте оборудование из каталога.'),
    ).toBeInTheDocument();
  });

  it('показывает пустое состояние для новой корзины', async () => {
    server.use(basketHandler(emptyBasket));

    renderWithProviders(<BasketPage />);

    expect(
      await screen.findByText('Корзина пуста. Добавьте оборудование из каталога.'),
    ).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'В каталог' })).toHaveAttribute('href', '/catalog');
  });

  it('сообщает об ошибке, если сервис корзины недоступен', async () => {
    server.use(
      http.get(basketApiUrl, () =>
        HttpResponse.json({ status: 503, title: 'Service Unavailable' }, { status: 503 }),
      ),
    );

    renderWithProviders(<BasketPage />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Не удалось обновить корзину');
  });
});

import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { Route, Routes } from 'react-router';
import { CheckoutPage } from '@/pages/CheckoutPage';
import { basketHandler, emptyBasket, orderApiUrl, sampleOrder } from '@/test/fixtures';
import { renderWithProviders } from '@/test/render';
import { server } from '@/test/server';

/** Оформление и страница статуса: путь «/orders/:number» подменён заглушкой. */
function renderCheckout() {
  return renderWithProviders(
    <Routes>
      <Route path="checkout" element={<CheckoutPage />} />
      <Route path="orders/:number" element={<p>Заявка оформлена</p>} />
    </Routes>,
    { route: '/checkout' },
  );
}

describe('CheckoutPage', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

  afterEach(() => server.resetHandlers());

  afterAll(() => server.close());

  it('показывает состав заявки из корзины', async () => {
    server.use(basketHandler());

    renderCheckout();

    expect(await screen.findByText('Состав заявки')).toBeInTheDocument();
    expect(screen.getByText('Установка регенерации TD-100 × 2')).toBeInTheDocument();
    expect(screen.getByLabelText('Удобный канал связи')).toHaveValue('Email');
    expect(screen.getByLabelText('Срочность')).toHaveValue('Standard');
  });

  it('проверяет обязательные поля и не отправляет заявку', async () => {
    const user = userEvent.setup();
    let posts = 0;

    server.use(
      basketHandler(),
      http.post(orderApiUrl, () => {
        posts += 1;

        return HttpResponse.json(sampleOrder(), { status: 201 });
      }),
    );

    renderCheckout();

    await screen.findByText('Состав заявки');
    await user.click(screen.getByRole('button', { name: 'Отправить заявку' }));

    expect(await screen.findByText('Укажите имя')).toBeInTheDocument();
    expect(screen.getByText('Укажите e-mail')).toBeInTheDocument();
    expect(posts).toBe(0);
  });

  it('отправляет контакты и переводит на страницу статуса заявки', async () => {
    const user = userEvent.setup();
    let body: Record<string, unknown> = {};

    server.use(
      basketHandler(),
      http.post(orderApiUrl, async ({ request }) => {
        body = (await request.json()) as Record<string, unknown>;

        return HttpResponse.json(sampleOrder(), { status: 201 });
      }),
    );

    renderCheckout();

    await screen.findByText('Состав заявки');

    await user.type(screen.getByLabelText('Имя и фамилия'), 'Иван Петров');
    await user.type(screen.getByLabelText('E-mail'), 'ivan@example.com');
    await user.type(screen.getByLabelText('Комментарий (необязательно)'), 'Нужен монтаж');
    await user.click(screen.getByRole('button', { name: 'Отправить заявку' }));

    expect(await screen.findByText('Заявка оформлена')).toBeInTheDocument();
    expect(body.customerName).toBe('Иван Петров');
    expect(body.customerEmail).toBe('ivan@example.com');
    expect(body.comment).toBe('Нужен монтаж');
    expect(body.preferredChannel).toBe('Email');
    expect(body.priority).toBe('Standard');
  });

  it('вместо формы подсказывает добавить товары, если корзина пуста', async () => {
    server.use(basketHandler(emptyBasket));

    renderCheckout();

    expect(
      await screen.findByText('Корзина пуста: сначала добавьте товары из каталога.'),
    ).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Отправить заявку' })).not.toBeInTheDocument();
  });

  it('показывает ошибку Order API, если заявку не удалось оформить', async () => {
    const user = userEvent.setup();

    server.use(
      basketHandler(),
      http.post(orderApiUrl, () =>
        HttpResponse.json(
          {
            status: 400,
            title: 'Ошибка валидации',
            detail: 'Корзина пуста — добавьте товары перед оформлением заявки.',
          },
          { status: 400 },
        ),
      ),
    );

    renderCheckout();

    await screen.findByText('Состав заявки');

    await user.type(screen.getByLabelText('Имя и фамилия'), 'Иван Петров');
    await user.type(screen.getByLabelText('E-mail'), 'ivan@example.com');
    await user.click(screen.getByRole('button', { name: 'Отправить заявку' }));

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Корзина пуста — добавьте товары перед оформлением заявки.',
    );
  });
});

import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { Route, Routes } from 'react-router';
import { OrderStatusPage } from '@/pages/OrderStatusPage';
import { orderApiUrl, sampleOrder } from '@/test/fixtures';
import { renderWithProviders } from '@/test/render';
import { server } from '@/test/server';

/** Страница статуса читает номер из адреса, поэтому путь задаёт сценарий. */
function renderOrderStatus(path = '/orders') {
  return renderWithProviders(
    <Routes>
      <Route path="orders" element={<OrderStatusPage />} />
      <Route path="orders/:number" element={<OrderStatusPage />} />
    </Routes>,
    { route: path },
  );
}

describe('OrderStatusPage', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

  afterEach(() => server.resetHandlers());

  afterAll(() => server.close());

  it('показывает форму поиска, если номер не задан', async () => {
    renderOrderStatus();

    expect(await screen.findByLabelText('Номер заявки')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Проверить' })).toBeInTheDocument();
  });

  it('переходит к заявке по введённому номеру', async () => {
    const user = userEvent.setup();

    server.use(
      http.get(`${orderApiUrl}/number/TD-20261004-00042`, () => HttpResponse.json(sampleOrder())),
    );

    renderOrderStatus();

    await user.type(await screen.findByLabelText('Номер заявки'), 'TD-20261004-00042');
    await user.click(screen.getByRole('button', { name: 'Проверить' }));

    expect(await screen.findByText('TD-20261004-00042')).toBeInTheDocument();
    // Текущий статус виден и в шапке карточки, и в воронке обработки.
    expect(screen.getAllByText('Ждёт менеджера')).toHaveLength(2);
  });

  it('показывает статус, позиции и контакты заявки', async () => {
    server.use(
      http.get(`${orderApiUrl}/number/TD-20261004-00042`, () => HttpResponse.json(sampleOrder())),
    );

    renderOrderStatus('/orders/TD-20261004-00042');

    expect(await screen.findByText('TD-20261004-00042')).toBeInTheDocument();
    expect(screen.getByText('Установка регенерации TD-100 × 2')).toBeInTheDocument();
    expect(screen.getByText('ivan@example.com')).toBeInTheDocument();
    expect(screen.getByText('+7 999 000-00-00')).toBeInTheDocument();
    // Воронка заявки: текущий статус выделен, остальные приглушены.
    expect(screen.getByText('Подтверждена')).toBeInTheDocument();
    expect(screen.getByText('Выполнена')).toBeInTheDocument();
  });

  it('показывает отмену заявки отдельным статусом', async () => {
    server.use(
      http.get(`${orderApiUrl}/number/TD-20261004-00042`, () =>
        HttpResponse.json(sampleOrder({ status: 'Cancelled' })),
      ),
    );

    renderOrderStatus('/orders/TD-20261004-00042');

    expect(await screen.findByText('Отменена')).toBeInTheDocument();
    expect(screen.queryByText('В работе')).not.toBeInTheDocument();
  });

  it('сообщает, что заявка не найдена', async () => {
    server.use(
      http.get(`${orderApiUrl}/number/TD-20261004-99999`, () =>
        HttpResponse.json({ status: 404, title: 'Не найдено' }, { status: 404 }),
      ),
    );

    renderOrderStatus('/orders/TD-20261004-99999');

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'Заявка не найдена. Проверьте номер.',
    );
  });
});

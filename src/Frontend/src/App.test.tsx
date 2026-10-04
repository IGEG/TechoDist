import { act, screen } from '@testing-library/react';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { App } from '@/App';
import { useAuthStore } from '@/features/auth/auth-store';
import {
  basketHandler,
  categoriesHandler,
  productsHandler,
  sampleSession,
} from '@/test/fixtures';
import { renderWithProviders } from '@/test/render';
import { server } from '@/test/server';

describe('App', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

  afterEach(() => {
    server.resetHandlers();
    // Сброс сессии трогает смонтированное дерево (RequireAuth/SiteHeader) — оборачиваем в act.
    act(() => useAuthStore.getState().clearSession());
  });

  afterAll(() => server.close());

  it('с главной переводит в каталог', async () => {
    server.use(productsHandler(), categoriesHandler(), basketHandler());

    renderWithProviders(<App />, { route: '/' });

    expect(
      await screen.findByRole('heading', { name: 'Каталог оборудования' }),
    ).toBeInTheDocument();
  });

  it('закрывает админ-панель и уводит на форму входа', async () => {
    server.use(basketHandler());

    renderWithProviders(<App />, { route: '/admin' });

    expect(
      await screen.findByRole('heading', { name: 'Вход в админ-панель' }),
    ).toBeInTheDocument();
    expect(screen.getByLabelText('E-mail')).toBeInTheDocument();
    expect(screen.getByLabelText('Пароль')).toBeInTheDocument();
  });

  it('показывает админ-панель сотруднику с ролью Manager', async () => {
    server.use(basketHandler());
    useAuthStore.getState().setSession(sampleSession(['Manager']));

    renderWithProviders(<App />, { route: '/admin' });

    expect(await screen.findByRole('heading', { name: 'Админ-панель' })).toBeInTheDocument();
    expect(screen.getByText('Иван Петров')).toBeInTheDocument();
    expect(screen.getByText('Manager')).toBeInTheDocument();
    // Товары и категории уже открыты: ссылки ведут в разделы каталога.
    expect(screen.getAllByRole('link', { name: 'Открыть раздел' })[0]).toHaveAttribute(
      'href',
      '/admin/products',
    );
  });

  it('открывает корзину гостя с бейджем в шапке', async () => {
    server.use(basketHandler(), categoriesHandler(), productsHandler());

    renderWithProviders(<App />, { route: '/cart' });

    expect(await screen.findByRole('heading', { name: 'Корзина' })).toBeInTheDocument();
    expect(screen.getByText('Итого: 2 500 ₽')).toBeInTheDocument();
    expect(screen.getByTestId('basket-link')).toHaveTextContent('Корзина');
    expect(screen.getByTestId('basket-link')).toHaveTextContent('2');
  });

  it('закрывает разделы админки без сессии', async () => {
    server.use(basketHandler());

    renderWithProviders(<App />, { route: '/admin/products' });

    expect(
      await screen.findByRole('heading', { name: 'Вход в админ-панель' }),
    ).toBeInTheDocument();
  });

  it('отдаёт страницу 404 на неизвестный адрес', async () => {
    server.use(basketHandler());

    renderWithProviders(<App />, { route: '/unknown-page' });

    expect(await screen.findByRole('heading', { name: 'Страница не найдена' })).toBeInTheDocument();
  });
});


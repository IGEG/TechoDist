import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import { HttpResponse, http } from 'msw';
import { act, type ReactNode } from 'react';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { API_BASE_URL } from '@/lib/api/client';
import { sampleSession } from '@/test/fixtures';
import { server } from '@/test/server';
import { createAccessToken } from '@/test/token';
import { ADMIN_CLIENT_ID } from './auth-api';
import { useAuthStore } from './auth-store';
import { useAuth, useHasAnyRole } from './use-auth';

const tokenEndpoint = `${API_BASE_URL}/identity/connect/token`;

/** Каждый хук получает свой QueryClient: кэш mutation не протекает между тестами. */
function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe('useAuth', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

  afterEach(() => {
    server.resetHandlers();
    // Сброс сессии трогает ещё смонтированный хук — без act React предупреждает об обновлении вне act.
    act(() => useAuthStore.getState().clearSession());
  });

  afterAll(() => server.close());

  it('сохраняет сессию и запрашивает токен сразу на нужные API', async () => {
    let requestBody = new URLSearchParams();

    server.use(
      http.post(tokenEndpoint, async ({ request }) => {
        requestBody = new URLSearchParams(await request.text());

        return HttpResponse.json({
          access_token: createAccessToken({ name: 'Иван Петров', role: ['Admin', 'Manager'] }),
          token_type: 'Bearer',
          expires_in: 3600,
          refresh_token: 'refresh-token',
        });
      }),
    );

    const { result } = renderHook(() => useAuth(), { wrapper: createWrapper() });

    await act(async () => {
      await result.current.login.mutateAsync({
        email: 'admin@techodist.local',
        password: 'Passw0rd!',
      });
    });

    expect(requestBody.get('grant_type')).toBe('password');
    expect(requestBody.get('username')).toBe('admin@techodist.local');
    expect(requestBody.get('client_id')).toBe(ADMIN_CLIENT_ID);
    expect(requestBody.get('scope')).toBe(
      'techodist-identity-api techodist-catalog-api offline_access',
    );
    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.session?.roles).toEqual(['Admin', 'Manager']);
    expect(useAuthStore.getState().session?.tokens.accessToken).toBeTruthy();
  });

  it('не создаёт сессию, если пароль неверный', async () => {
    server.use(
      http.post(tokenEndpoint, () =>
        HttpResponse.json({ error: 'invalid_grant', error_description: 'Invalid credentials' }, { status: 400 }),
      ),
    );

    const { result } = renderHook(() => useAuth(), { wrapper: createWrapper() });

    await act(async () => {
      await result.current.login
        .mutateAsync({ email: 'admin@techodist.local', password: 'wrong' })
        .catch(() => undefined);
    });

    await waitFor(() => expect(result.current.login.isError).toBe(true));
    expect(result.current.isAuthenticated).toBe(false);
    expect(useAuthStore.getState().session).toBeNull();
  });

  it('выход очищает сессию', () => {
    useAuthStore.getState().setSession(sampleSession(['Admin']));

    const { result } = renderHook(() => useAuth(), { wrapper: createWrapper() });

    expect(result.current.isAuthenticated).toBe(true);

    act(() => result.current.logout());

    expect(result.current.isAuthenticated).toBe(false);
    expect(useAuthStore.getState().session).toBeNull();
  });
});

describe('useHasAnyRole', () => {
  afterEach(() => {
    act(() => useAuthStore.getState().clearSession());
  });

  it('подтверждает доступ по роли из сессии', () => {
    useAuthStore.getState().setSession(sampleSession(['Manager']));

    const { result } = renderHook(() => useHasAnyRole('Admin', 'Manager'));

    expect(result.current).toBe(true);
  });

  it('не подтверждает доступ без нужной роли', () => {
    useAuthStore.getState().setSession(sampleSession(['Manager']));

    const { result } = renderHook(() => useHasAnyRole('Admin'));

    expect(result.current).toBe(false);
  });
});

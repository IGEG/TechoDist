import { HttpResponse, http } from 'msw';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { API_BASE_URL } from '@/lib/api/client';
import { server } from '@/test/server';
import { createAccessToken } from '@/test/token';
import { getFreshAccessToken, readAccessToken, selectSession, useAuthStore } from './auth-store';
import type { AuthSession } from './types';

const tokenEndpoint = `${API_BASE_URL}/identity/connect/token`;

const session: AuthSession = {
  tokens: { accessToken: 'access-token', refreshToken: null, expiresAt: null },
  displayName: 'Иван Петров',
  email: 'admin@techodist.local',
  roles: ['Admin'],
};

/** Сессия с заданным сроком жизни access-токена (для проверки продления). */
function sessionWithExpiry(expiresAt: number | null): AuthSession {
  return {
    ...session,
    tokens: { accessToken: 'old-access-token', refreshToken: 'refresh-token', expiresAt },
  };
}

describe('auth-store', () => {
  afterEach(() => {
    useAuthStore.getState().clearSession();
    localStorage.clear();
  });

  it('хранит сессию и отдаёт access-токен HTTP-клиенту', () => {
    useAuthStore.getState().setSession(session);

    expect(selectSession(useAuthStore.getState())).toEqual(session);
    expect(readAccessToken()).toBe('access-token');
  });

  it('очищает сессию при выходе', () => {
    useAuthStore.getState().setSession(session);
    useAuthStore.getState().clearSession();

    expect(selectSession(useAuthStore.getState())).toBeNull();
    expect(readAccessToken()).toBeNull();
  });

  it('сохраняет сессию в localStorage: перезагрузка страницы не разлогинивает', () => {
    useAuthStore.getState().setSession(session);

    const persisted = JSON.parse(localStorage.getItem('techodist.auth') ?? 'null') as {
      state?: { session?: AuthSession };
    };

    expect(persisted.state?.session?.tokens.accessToken).toBe('access-token');
  });
});

describe('getFreshAccessToken', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

  afterEach(() => {
    server.resetHandlers();
    useAuthStore.getState().clearSession();
  });

  afterAll(() => server.close());

  it('отдаёт текущий токен без обращения к Identity, пока срок не истекает', async () => {
    useAuthStore.getState().setSession(sessionWithExpiry(Date.now() + 3_600_000));

    // Запрос к token-endpoint не зарегистрирован: onUnhandledRequest: 'error' поймает лишний вызов.
    await expect(getFreshAccessToken()).resolves.toBe('old-access-token');
  });

  it('продлевает сессию по refresh-токену, когда access-токен истекает', async () => {
    let requestBody = new URLSearchParams();

    server.use(
      http.post(tokenEndpoint, async ({ request }) => {
        requestBody = new URLSearchParams(await request.text());

        return HttpResponse.json({
          access_token: createAccessToken({ name: 'Иван Петров', role: 'Admin' }),
          refresh_token: 'rotated-refresh-token',
          token_type: 'Bearer',
          expires_in: 3600,
        });
      }),
    );

    useAuthStore.getState().setSession(sessionWithExpiry(Date.now() + 1_000));

    const token = await getFreshAccessToken();

    expect(requestBody.get('grant_type')).toBe('refresh_token');
    expect(requestBody.get('refresh_token')).toBe('refresh-token');
    expect(token).toBeTruthy();
    expect(token).not.toBe('old-access-token');
    expect(useAuthStore.getState().session?.tokens.accessToken).toBe(token);
  });

  it('делит один обмен токена между параллельными запросами', async () => {
    let calls = 0;

    server.use(
      http.post(tokenEndpoint, () => {
        calls += 1;

        return HttpResponse.json({
          access_token: createAccessToken({ name: 'Иван Петров', role: 'Admin' }),
          token_type: 'Bearer',
          expires_in: 3600,
        });
      }),
    );

    useAuthStore.getState().setSession(sessionWithExpiry(Date.now() + 1_000));

    const [first, second] = await Promise.all([getFreshAccessToken(), getFreshAccessToken()]);

    expect(calls).toBe(1);
    expect(first).toBe(second);
  });

  it('сбрасывает сессию, если refresh-токен отклонён', async () => {
    server.use(
      http.post(tokenEndpoint, () =>
        HttpResponse.json({ error: 'invalid_grant' }, { status: 400 }),
      ),
    );

    useAuthStore.getState().setSession(sessionWithExpiry(Date.now() + 1_000));

    await expect(getFreshAccessToken()).resolves.toBeNull();
    expect(useAuthStore.getState().session).toBeNull();
  });
});

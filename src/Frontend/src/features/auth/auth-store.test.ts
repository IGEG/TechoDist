import { afterEach, describe, expect, it } from 'vitest';
import { getAccessToken, selectSession, useAuthStore } from './auth-store';
import type { AuthSession } from './types';

const session: AuthSession = {
  tokens: { accessToken: 'access-token', refreshToken: null, expiresAt: null },
  displayName: 'Иван Петров',
  email: 'admin@techodist.local',
  roles: ['Admin'],
};

describe('auth-store', () => {
  afterEach(() => {
    useAuthStore.getState().clearSession();
    localStorage.clear();
  });

  it('хранит сессию и отдаёт access-токен HTTP-клиенту', () => {
    useAuthStore.getState().setSession(session);

    expect(selectSession(useAuthStore.getState())).toEqual(session);
    expect(getAccessToken()).toBe('access-token');
  });

  it('очищает сессию при выходе', () => {
    useAuthStore.getState().setSession(session);
    useAuthStore.getState().clearSession();

    expect(selectSession(useAuthStore.getState())).toBeNull();
    expect(getAccessToken()).toBeNull();
  });

  it('сохраняет сессию в localStorage: перезагрузка страницы не разлогинивает', () => {
    useAuthStore.getState().setSession(session);

    const persisted = JSON.parse(localStorage.getItem('techodist.auth') ?? 'null') as {
      state?: { session?: AuthSession };
    };

    expect(persisted.state?.session?.tokens.accessToken).toBe('access-token');
  });
});

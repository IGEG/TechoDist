import { AxiosError, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios';
import { describe, expect, it } from 'vitest';
import { createAccessToken } from '@/test/token';
import { toLoginErrorMessage, toSession } from './auth-api';
import type { TokenResponse } from './types';

function tokenResponse(accessToken: string, overrides: Partial<TokenResponse> = {}): TokenResponse {
  return {
    access_token: accessToken,
    refresh_token: 'refresh-token-value',
    token_type: 'Bearer',
    expires_in: 3600,
    ...overrides,
  };
}

function axiosErrorWith(status: number, data?: unknown): AxiosError {
  const config = {} as InternalAxiosRequestConfig;
  const response = { status, statusText: '', headers: {}, config, data } as AxiosResponse;

  return new AxiosError('Request failed', AxiosError.ERR_BAD_REQUEST, config, undefined, response);
}

describe('toSession', () => {
  it('переносит токены и claims администратора в сессию панели', () => {
    const response = tokenResponse(
      createAccessToken({
        name: 'Иван Петров',
        email: 'admin@techodist.local',
        role: ['Admin', 'Manager'],
      }),
    );

    const session = toSession(response);

    expect(session.tokens.accessToken).toBe(response.access_token);
    expect(session.tokens.refreshToken).toBe('refresh-token-value');
    expect(session.tokens.expiresAt).toBeGreaterThan(Date.now());
    expect(session.displayName).toBe('Иван Петров');
    expect(session.email).toBe('admin@techodist.local');
    expect(session.roles).toEqual(['Admin', 'Manager']);
  });

  it('принимает одну роль строкой — OpenIddict отдаёт так, если роль единственная', () => {
    const session = toSession(tokenResponse(createAccessToken({ role: 'Manager' })));

    expect(session.roles).toEqual(['Manager']);
  });

  it('берёт e-mail вместо имени и не падает без claims', () => {
    const withEmail = toSession(tokenResponse(createAccessToken({ email: 'manager@techodist.local' })));
    const withoutClaims = toSession(tokenResponse('not-a-jwt'));

    expect(withEmail.displayName).toBe('manager@techodist.local');
    expect(withoutClaims.displayName).toBe('Администратор');
    expect(withoutClaims.roles).toEqual([]);
    expect(withoutClaims.email).toBeNull();
  });

  it('оставляет expiresAt пустым, если сервер не сообщил expires_in', () => {
    const session = toSession(tokenResponse(createAccessToken({ name: 'Админ' }), { expires_in: 0 }));

    expect(session.tokens.expiresAt).toBeNull();
  });
});

describe('toLoginErrorMessage', () => {
  it('объясняет неверные учётные данные', () => {
    expect(toLoginErrorMessage(axiosErrorWith(400, { error: 'invalid_grant' }))).toBe(
      'Неверный e-mail или пароль.',
    );
  });

  it('сообщает о незарегистрированном клиенте админ-панели', () => {
    expect(toLoginErrorMessage(axiosErrorWith(401, { error: 'invalid_client' }))).toBe(
      'Админ-панель не зарегистрирована как клиент Identity.',
    );
  });

  it('показывает error_description от Identity', () => {
    expect(
      toLoginErrorMessage(axiosErrorWith(400, { error: 'invalid_scope', error_description: 'Unknown scope' })),
    ).toBe('Unknown scope');
  });

  it('поясняет троттлинг и недоступный шлюз', () => {
    expect(toLoginErrorMessage(axiosErrorWith(429))).toBe(
      'Слишком много попыток входа. Повторите через минуту.',
    );

    const networkError = new AxiosError('Network Error', AxiosError.ERR_NETWORK);

    expect(toLoginErrorMessage(networkError)).toContain('API Gateway недоступен');
  });

  it('на неизвестной ошибке возвращает общий текст', () => {
    expect(toLoginErrorMessage(new Error('boom'))).toBe('Не удалось выполнить вход. Попробуйте позже.');
  });
});

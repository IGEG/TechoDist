import axios, { AxiosError } from 'axios';
import { API_BASE_URL } from '@/lib/api/client';
import { decodeAccessToken, normalizeRoles } from './jwt';
import type { AuthSession, TokenResponse } from './types';

/**
 * Админ-панель — единственный клиент Identity (ADR 0004): confidential-клиент,
 * потоки password + refresh_token. Значения по умолчанию совпадают с сидером Identity
 * (OpenIddictDataSeeder) и переопределяются переменными окружения.
 */
export const ADMIN_CLIENT_ID: string = import.meta.env.VITE_ADMIN_CLIENT_ID ?? 'techodist-admin-panel';

const ADMIN_CLIENT_SECRET: string =
  import.meta.env.VITE_ADMIN_CLIENT_SECRET ?? 'techodist-admin-panel-dev-secret';

/**
 * scope = аудитория сервиса. Один access-токен запрашивается сразу на нужные API:
 * так админ-панель ходит и в Identity (профиль/пользователи), и в Catalog (товары/категории).
 */
export const REQUESTED_SCOPES: string[] = [
  'techodist-identity-api',
  'techodist-catalog-api',
  'offline_access',
];

const TOKEN_ENDPOINT = `${API_BASE_URL}/identity/connect/token`;

async function requestToken(parameters: Record<string, string>): Promise<AuthSession> {
  const body = new URLSearchParams({
    client_id: ADMIN_CLIENT_ID,
    client_secret: ADMIN_CLIENT_SECRET,
    ...parameters,
  });

  const { data } = await axios.post<TokenResponse>(TOKEN_ENDPOINT, body, {
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
  });

  return toSession(data);
}

/** Вход администратора: grant_type=password. */
export function loginWithPassword(email: string, password: string): Promise<AuthSession> {
  return requestToken({
    grant_type: 'password',
    username: email,
    password,
    scope: REQUESTED_SCOPES.join(' '),
  });
}

/** Продление сессии: identity перечитывает профиль из БД, поэтому отключение учётки действует сразу. */
export function refreshSession(refreshToken: string): Promise<AuthSession> {
  return requestToken({ grant_type: 'refresh_token', refresh_token: refreshToken });
}

/** Имя и роли берём из claims access-токена — отдельный вызов профиля при входе не нужен. */
export function toSession(response: TokenResponse): AuthSession {
  const claims = decodeAccessToken(response.access_token) ?? {};

  return {
    tokens: {
      accessToken: response.access_token,
      refreshToken: response.refresh_token ?? null,
      expiresAt: response.expires_in ? Date.now() + response.expires_in * 1000 : null,
    },
    displayName: claims.name ?? claims.email ?? 'Администратор',
    email: claims.email ?? null,
    roles: normalizeRoles(claims.role),
  };
}

/** Текст ошибки входа: token-endpoint отвечает по спецификации OAuth 2.0 (error/error_description). */
export function toLoginErrorMessage(error: unknown): string {
  if (error instanceof AxiosError) {
    const data = error.response?.data as { error?: string; error_description?: string } | undefined;

    if (data?.error === 'invalid_grant') {
      return 'Неверный e-mail или пароль.';
    }

    if (data?.error === 'invalid_client') {
      return 'Админ-панель не зарегистрирована как клиент Identity.';
    }

    if (data?.error_description) {
      return data.error_description;
    }

    if (error.response?.status === 429) {
      return 'Слишком много попыток входа. Повторите через минуту.';
    }

    if (error.response?.status === 0 || error.code === 'ERR_NETWORK') {
      return 'API Gateway недоступен: запущен ли он на ' + API_BASE_URL + '?';
    }
  }

  return 'Не удалось выполнить вход. Попробуйте позже.';
}

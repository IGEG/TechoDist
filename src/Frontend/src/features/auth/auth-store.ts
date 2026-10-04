import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { configureApiAuth } from '@/lib/api/client';
import { refreshSession } from './auth-api';
import type { AuthSession } from './types';

interface AuthState {
  session: AuthSession | null;
  setSession: (session: AuthSession) => void;
  clearSession: () => void;
}

/** Запас до истечения access-токена: продлеваем заранее, чтобы запрос успел уйти с новым токеном. */
const TOKEN_REFRESH_SKEW_MS = 60_000;

/**
 * Сессия администратора в localStorage (persist): перезагрузка страницы не разлогинивает.
 * Срок access-токена продлевается по refresh-токену (см. getFreshAccessToken) — так сотрудник
 * не вылетает из панели, пока открыт хотя бы один экран.
 */
export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      session: null,
      setSession: (session) => set({ session }),
      clearSession: () => set({ session: null }),
    }),
    { name: 'techodist.auth' },
  ),
);

export const selectSession = (state: AuthState): AuthSession | null => state.session;

/** Синхронное чтение access-токена из store (без обращения к Identity). */
export function readAccessToken(): string | null {
  return useAuthStore.getState().session?.tokens.accessToken ?? null;
}

/** Токен пора продлевать: refresh-токен есть, а access-токен истёк или истекает. */
function needsRefresh(session: AuthSession): boolean {
  const { refreshToken, expiresAt } = session.tokens;

  return (
    refreshToken !== null && expiresAt !== null && expiresAt - Date.now() <= TOKEN_REFRESH_SKEW_MS
  );
}

/** Незавершённый обмен refresh-токена: параллельные запросы ждут один и тот же (single-flight). */
let refreshInFlight: Promise<string | null> | null = null;

/**
 * Access-токен для очередного запроса. Если access-токен на исходе, продлеваем сессию
 * по refresh-токену; при неудаче сессия сбрасывается и запрос уходит без токена (guard
 * отправит сотрудника на форму входа).
 */
export function getFreshAccessToken(): Promise<string | null> {
  const session = useAuthStore.getState().session;

  if (!session) {
    return Promise.resolve(null);
  }

  if (!needsRefresh(session)) {
    return Promise.resolve(session.tokens.accessToken);
  }

  refreshInFlight ??= refreshSession(session.tokens.refreshToken as string)
    .then((next) => {
      useAuthStore.getState().setSession(next);

      return next.tokens.accessToken;
    })
    .catch(() => {
      useAuthStore.getState().clearSession();

      return null;
    })
    .finally(() => {
      refreshInFlight = null;
    });

  return refreshInFlight;
}

configureApiAuth({
  getAccessToken: getFreshAccessToken,
  // 401 от любого сервиса = сессия недействительна (истёк или отозван токен) → выходим.
  onUnauthorized: () => useAuthStore.getState().clearSession(),
});

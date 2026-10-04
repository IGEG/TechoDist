import { useMutation } from '@tanstack/react-query';
import { loginWithPassword } from './auth-api';
import { useAuthStore } from './auth-store';
import { selectSession } from './auth-store';
import type { AuthSession } from './types';

export interface LoginCredentials {
  email: string;
  password: string;
}

/**
 * Состояние админ-панели: сессия из store + mutation входа.
 * Токены в store, а HTTP-клиент получает их через configureApiAuth (см. auth-store).
 */
export function useAuth() {
  const session = useAuthStore(selectSession);
  const setSession = useAuthStore((state) => state.setSession);
  const clearSession = useAuthStore((state) => state.clearSession);

  const login = useMutation<AuthSession, unknown, LoginCredentials>({
    mutationFn: ({ email, password }) => loginWithPassword(email, password),
    onSuccess: setSession,
  });

  return {
    session,
    isAuthenticated: session !== null,
    roles: session?.roles ?? [],
    login,
    logout: clearSession,
  };
}

/** Проверка роли без подписки на всю сессию. */
export function useHasAnyRole(...roles: string[]): boolean {
  const session = useAuthStore(selectSession);

  return session !== null && roles.some((role) => session.roles.includes(role));
}

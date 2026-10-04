import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { configureApiAuth } from '@/lib/api/client';
import type { AuthSession } from './types';

interface AuthState {
  session: AuthSession | null;
  setSession: (session: AuthSession) => void;
  clearSession: () => void;
}

/**
 * Сессия администратора в localStorage (persist): перезагрузка страницы не разлогинивает.
 * Refresh-токен продлевается в phase 8; пока истёкший access-токен приводит к выходу.
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

/** Текущий access-токен — его подставляет HTTP-клиент (см. configureApiAuth ниже). */
export function getAccessToken(): string | null {
  return useAuthStore.getState().session?.tokens.accessToken ?? null;
}

configureApiAuth({
  getAccessToken,
  // 401 от любого сервиса = сессия недействительна (истёк или отозван токен) → выходим.
  onUnauthorized: () => useAuthStore.getState().clearSession(),
});

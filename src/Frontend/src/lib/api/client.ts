import axios, { AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios';

/**
 * Единственная точка входа в backend — API Gateway (YARP).
 * Отдельные сервисы наружу не публикуются, поэтому префикс сервиса (/catalog, /identity)
 * добавляется к адресу прямо в запросах.
 */
export const API_BASE_URL: string = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5100';

/** ProblemDetails от GlobalExceptionHandler / ValidationExceptionHandler. */
export interface ApiProblem {
  status: number;
  title: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

interface AuthHooks {
  /**
   * Access-токен для очередного запроса. Разрешён асинхронный результат: перед запросом
   * клиент может продлить сессию по refresh-токену (см. features/auth/auth-store.ts).
   */
  getAccessToken: () => string | null | Promise<string | null>;
  onUnauthorized?: () => void;
}

let authHooks: AuthHooks = { getAccessToken: () => null };

/** Подключает store с токенами: клиент не должен знать про Zustand (auth-store вызывает это сам). */
export function configureApiAuth(hooks: AuthHooks): void {
  authHooks = hooks;
}

export const apiClient: AxiosInstance = axios.create({
  baseURL: API_BASE_URL,
  headers: { Accept: 'application/json' },
  // Гостевая корзина живёт в анонимном HttpOnly-cookie (ADR 0005): без withCredentials
  // браузер не отправит его на кросс-origin запрос к шлюзу, и корзина каждый раз будет новой.
  // Шлюз отвечает `Access-Control-Allow-Credentials: true` (policy techodist).
  withCredentials: true,
});

apiClient.interceptors.request.use(async (config: InternalAxiosRequestConfig) => {
  const token = await authHooks.getAccessToken();

  if (token) {
    config.headers.set('Authorization', `Bearer ${token}`);
  }

  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    // Просроченный/отозванный токен: сессию сбрасывает auth-store, дальше сработает guard.
    if (error instanceof AxiosError && error.response?.status === 401) {
      authHooks.onUnauthorized?.();
    }

    return Promise.reject(error);
  },
);

/** Приводит ошибку клиента к ProblemDetails: формы показывают пользователю готовый текст. */
export function toApiProblem(error: unknown): ApiProblem {
  if (error instanceof AxiosError) {
    const data = error.response?.data as Partial<ApiProblem> | undefined;

    return {
      status: error.response?.status ?? 0,
      title: data?.title ?? error.message,
      detail: data?.detail,
      errors: data?.errors,
    };
  }

  return {
    status: 0,
    title: error instanceof Error ? error.message : 'Неизвестная ошибка',
  };
}

/** Человекочитаемый текст ошибки: сначала ошибки валидации, потом detail, потом title. */
export function describeApiProblem(problem: ApiProblem): string {
  const validationMessage = problem.errors ? Object.values(problem.errors).flat()[0] : undefined;

  return validationMessage ?? problem.detail ?? problem.title;
}

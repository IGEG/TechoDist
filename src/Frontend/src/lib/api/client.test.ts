import { HttpResponse, http } from 'msw';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { server } from '@/test/server';
import { API_BASE_URL, apiClient, describeApiProblem, toApiProblem } from './client';

// Хуки на уровне файла: оба блока describe ходят через MSW, а afterAll в первом из них
// закрыл бы сервер раньше второго.
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));

afterEach(() => server.resetHandlers());

afterAll(() => server.close());

describe('apiClient', () => {
  it('ходит только через шлюз: базовый адрес клиента — адрес YARP', () => {
    expect(apiClient.defaults.baseURL).toBe(API_BASE_URL);
  });

  it('отправляет cookie гостевой корзины: withCredentials включён (ADR 0005)', async () => {
    let credentials: RequestCredentials | undefined;

    server.use(
      http.get(`${API_BASE_URL}/basket/api/items`, ({ request }) => {
        credentials = request.credentials;

        return HttpResponse.json({ items: [] });
      }),
    );

    await apiClient.get('/basket/api/items');

    // Без withCredentials браузер не отправит HttpOnly-cookie `techodist_basket` на кросс-origin запрос.
    expect(apiClient.defaults.withCredentials).toBe(true);
    expect(credentials).toBe('include');
  });
});

describe('toApiProblem', () => {
  it('разбирает ProblemDetails с ошибками валидации', async () => {
    server.use(
      http.post(`${API_BASE_URL}/basket/api/items`, () =>
        HttpResponse.json(
          {
            status: 400,
            title: 'Ошибка валидации',
            errors: { Quantity: ['Количество вне диапазона'] },
          },
          { status: 400 },
        ),
      ),
    );

    const error = await apiClient
      .post('/basket/api/items', { productId: 'p-1', quantity: 1000 })
      .catch((reason: unknown) => reason);
    const problem = toApiProblem(error);

    expect(problem.status).toBe(400);
    expect(problem.title).toBe('Ошибка валидации');
    expect(describeApiProblem(problem)).toBe('Количество вне диапазона');
  });

  it('для неизвестной ошибки отдаёт её сообщение', () => {
    const problem = toApiProblem(new Error('Network Error'));

    expect(problem.status).toBe(0);
    expect(describeApiProblem(problem)).toBe('Network Error');
  });
});


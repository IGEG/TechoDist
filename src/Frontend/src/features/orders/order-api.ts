import { apiClient } from '@/lib/api/client';
import type { Order, SubmitOrderInput } from '@/lib/api/types';

/**
 * Order API доступен только через шлюз (ADR 0007): шлюз снимает префикс `/order`.
 * Гость умеет оформить заявку из своей корзины и прочитать её статус по номеру.
 */
const ORDER_PREFIX = '/order/api/orders';

/** Оформление заявки: позиции берутся из гостевой корзины по cookie (ADR 0005). */
export async function submitOrder(input: SubmitOrderInput): Promise<Order> {
  const { data } = await apiClient.post<Order>(ORDER_PREFIX, input);

  return data;
}

/** Статус заявки по читаемому номеру из письма (например, TD-20261004-00042). */
export async function fetchOrderByNumber(number: string): Promise<Order> {
  const { data } = await apiClient.get<Order>(
    `${ORDER_PREFIX}/number/${encodeURIComponent(number)}`,
  );

  return data;
}

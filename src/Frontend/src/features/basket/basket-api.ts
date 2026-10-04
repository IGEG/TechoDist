import { apiClient } from '@/lib/api/client';
import type { Basket } from '@/lib/api/types';

/**
 * Basket API доступен только через шлюз (ADR 0007): шлюз снимает префикс `/basket`
 * перед проксированием, поэтому сервис видит `/api/basket`.
 * Токена здесь нет: корзина привязана к анонимному HttpOnly-cookie гостя (ADR 0005).
 */
const BASKET_PREFIX = '/basket/api/basket';

/** Максимальное количество единиц одного товара — совпадает с BasketItem.MaxQuantity. */
export const MAX_BASKET_ITEM_QUANTITY = 99;

/** Текущая корзина гостя (пустой объект, если корзины ещё нет). */
export async function fetchBasket(): Promise<Basket> {
  const { data } = await apiClient.get<Basket>(BASKET_PREFIX);

  return data;
}

/** Добавление товара: сервис сам берёт снимок названия и цены из Catalog. */
export async function addBasketItem(productId: string, quantity = 1): Promise<Basket> {
  const { data } = await apiClient.post<Basket>(`${BASKET_PREFIX}/items`, { productId, quantity });

  return data;
}

/** Изменение количества позиции. */
export async function updateBasketItemQuantity(productId: string, quantity: number): Promise<Basket> {
  const { data } = await apiClient.put<Basket>(`${BASKET_PREFIX}/items/${productId}`, { quantity });

  return data;
}

/** Удаление позиции из корзины. */
export async function removeBasketItem(productId: string): Promise<Basket> {
  const { data } = await apiClient.delete<Basket>(`${BASKET_PREFIX}/items/${productId}`);

  return data;
}

/** Полная очистка корзины. */
export async function clearBasket(): Promise<Basket> {
  const { data } = await apiClient.delete<Basket>(BASKET_PREFIX);

  return data;
}

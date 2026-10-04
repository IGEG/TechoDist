import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { Basket } from '@/lib/api/types';
import {
  addBasketItem,
  clearBasket,
  fetchBasket,
  removeBasketItem,
  updateBasketItemQuantity,
} from './basket-api';

const basketRoot = ['basket'] as const;

/** Ключи кэша корзины: одна корзина на гостя (её id живёт в cookie, а не в адресе). */
export const basketKeys = {
  all: basketRoot,
  current: () => [...basketRoot, 'current'] as const,
};

/** Текущая корзина. Тот же запрос питает бейдж в шапке и страницу корзины. */
export function useBasket() {
  return useQuery({
    queryKey: basketKeys.current(),
    queryFn: fetchBasket,
  });
}

/**
 * Общая часть мутаций корзины: сервер возвращает корзину целиком, поэтому сразу кладём её
 * в кэш — бейдж и страница обновляются без повторного GET (и без «мигания»).
 */
function useBasketMutation<TVariables>(mutationFn: (variables: TVariables) => Promise<Basket>) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn,
    onSuccess: (basket) => queryClient.setQueryData(basketKeys.current(), basket),
  });
}

export interface AddBasketItemVariables {
  productId: string;
  quantity?: number;
}

export function useAddBasketItem() {
  return useBasketMutation(({ productId, quantity = 1 }: AddBasketItemVariables) =>
    addBasketItem(productId, quantity),
  );
}

export interface UpdateBasketItemVariables {
  productId: string;
  quantity: number;
}

export function useUpdateBasketItemQuantity() {
  return useBasketMutation(({ productId, quantity }: UpdateBasketItemVariables) =>
    updateBasketItemQuantity(productId, quantity),
  );
}

export function useRemoveBasketItem() {
  return useBasketMutation((productId: string) => removeBasketItem(productId));
}

export function useClearBasket() {
  return useBasketMutation(() => clearBasket());
}

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { basketKeys } from '@/features/basket/use-basket';
import type { Order, SubmitOrderInput } from '@/lib/api/types';
import { fetchOrderByNumber, submitOrder } from './order-api';

const orderRoot = ['orders'] as const;

export const orderKeys = {
  all: orderRoot,
  byNumber: (number: string) => [...orderRoot, 'number', number] as const,
};

/** Заявка по номеру: запрос публичный, пока включён номер (гость знает его из письма). */
export function useOrderByNumber(number: string | undefined) {
  return useQuery({
    queryKey: orderKeys.byNumber(number ?? ''),
    queryFn: () => fetchOrderByNumber(number as string),
    enabled: Boolean(number),
  });
}

/** Оформление заявки из гостевой корзины. */
export function useSubmitOrder() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (input: SubmitOrderInput) => submitOrder(input),
    onSuccess: (order: Order) => {
      // Заявку кладём в кэш по номеру: страница статуса откроется без повторного запроса.
      queryClient.setQueryData(orderKeys.byNumber(order.number), order);

      // Order очищает корзину после успешного оформления (best-effort), поэтому состав корзины
      // перечитываем из Basket, а не правим вручную.
      void queryClient.invalidateQueries({ queryKey: basketKeys.all });
    },
  });
}

import { keepPreviousData, useQuery } from '@tanstack/react-query';
import type { ProductListParams } from '@/lib/api/types';
import { fetchCategories, fetchProduct, fetchProducts } from './catalog-api';

const catalogRoot = ['catalog'] as const;

/** Ключи кэша TanStack Query: параметры списка входят в ключ, поэтому страницы кэшируются раздельно. */
export const catalogKeys = {
  all: catalogRoot,
  products: (params: ProductListParams) => [...catalogRoot, 'products', params] as const,
  product: (id: string) => [...catalogRoot, 'product', id] as const,
  categories: () => [...catalogRoot, 'categories'] as const,
};

export function useProducts(params: ProductListParams) {
  return useQuery({
    queryKey: catalogKeys.products(params),
    queryFn: () => fetchProducts(params),
    // Пагинация без «мигания»: старые данные видны, пока грузятся новые.
    placeholderData: keepPreviousData,
  });
}

export function useCategories() {
  return useQuery({
    queryKey: catalogKeys.categories(),
    queryFn: fetchCategories,
  });
}

export function useProduct(id: string | undefined) {
  return useQuery({
    queryKey: catalogKeys.product(id ?? ''),
    queryFn: () => fetchProduct(id as string),
    enabled: Boolean(id),
  });
}

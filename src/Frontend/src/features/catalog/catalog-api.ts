import { apiClient } from '@/lib/api/client';
import type {
  Category,
  PagedResult,
  ProductDetails,
  ProductListParams,
  ProductSummary,
} from '@/lib/api/types';

/**
 * Catalog API доступен только через шлюз (ADR 0007): префикс сервиса — часть маршрута,
 * шлюз сам снимает его перед отправкой в сервис.
 */
const CATALOG_PREFIX = '/catalog/api';

/** Постраничный список опубликованных товаров с фильтрами. */
export async function fetchProducts(params: ProductListParams = {}): Promise<PagedResult<ProductSummary>> {
  const { data } = await apiClient.get<PagedResult<ProductSummary>>(`${CATALOG_PREFIX}/products`, {
    params: { page: 1, pageSize: 12, ...params },
  });

  return data;
}

/** Опубликованные категории каталога. */
export async function fetchCategories(): Promise<Category[]> {
  const { data } = await apiClient.get<Category[]>(`${CATALOG_PREFIX}/categories`, {
    params: { onlyPublished: true },
  });

  return data;
}

/** Детальная карточка товара по идентификатору. */
export async function fetchProduct(id: string): Promise<ProductDetails> {
  const { data } = await apiClient.get<ProductDetails>(`${CATALOG_PREFIX}/products/${id}`);

  return data;
}

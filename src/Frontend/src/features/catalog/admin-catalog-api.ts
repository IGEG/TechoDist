import { apiClient } from '@/lib/api/client';
import type {
  AdminProductListParams,
  Category,
  CategoryWriteInput,
  PagedResult,
  ProductSummary,
  ProductWriteInput,
} from '@/lib/api/types';

/**
 * Админские операции каталога. Идут через шлюз теми же префиксами, что и витрина,
 * но все изменяющие вызовы закрыты ролью Admin/Manager на шлюзе и в сервисе (ADR 0004).
 */
const CATALOG_PREFIX = '/catalog/api';

/** Список товаров для админки: включает черновики и архив. */
export async function fetchAdminProducts(
  params: AdminProductListParams = {},
): Promise<PagedResult<ProductSummary>> {
  const { data } = await apiClient.get<PagedResult<ProductSummary>>(
    `${CATALOG_PREFIX}/products/admin`,
    { params: { page: 1, pageSize: 20, ...params } },
  );

  return data;
}

/** Все категории, включая неопубликованные: админка правит структуру каталога. */
export async function fetchAdminCategories(): Promise<Category[]> {
  const { data } = await apiClient.get<Category[]>(`${CATALOG_PREFIX}/categories`, {
    params: { onlyPublished: false },
  });

  return data;
}

/** Создание товара: возвращает идентификатор новой карточки (черновик). */
export async function createProduct(input: ProductWriteInput): Promise<string> {
  const { data } = await apiClient.post<string>(`${CATALOG_PREFIX}/products`, input);

  return data;
}

/** Изменение товара: сервис публикует событие ProductChanged → индекс поиска (ADR 0009). */
export async function updateProduct(id: string, input: ProductWriteInput): Promise<void> {
  await apiClient.put(`${CATALOG_PREFIX}/products/${id}`, input);
}

/** Публикация: карточка появляется на витрине и в поиске. */
export async function publishProduct(id: string): Promise<string> {
  const { data } = await apiClient.post<string>(`${CATALOG_PREFIX}/products/${id}/publish`);

  return data;
}

/** Снятие с продажи (архив): карточка уходит с витрины и из поиска. */
export async function archiveProduct(id: string): Promise<string> {
  const { data } = await apiClient.post<string>(`${CATALOG_PREFIX}/products/${id}/archive`);

  return data;
}

/** Удаление черновика: опубликованный товар сервис удалить не даст (нужен архив). */
export async function deleteProduct(id: string): Promise<void> {
  await apiClient.delete(`${CATALOG_PREFIX}/products/${id}`);
}

/** Создание категории: возвращает идентификатор новой категории. */
export async function createCategory(input: CategoryWriteInput): Promise<string> {
  const { data } = await apiClient.post<string>(`${CATALOG_PREFIX}/categories`, input);

  return data;
}

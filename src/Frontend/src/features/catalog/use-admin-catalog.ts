import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
  type QueryClient,
} from '@tanstack/react-query';
import type { AdminProductListParams, CategoryWriteInput, ProductWriteInput } from '@/lib/api/types';
import {
  archiveProduct,
  createCategory,
  createProduct,
  deleteProduct,
  fetchAdminCategories,
  fetchAdminProducts,
  publishProduct,
  updateProduct,
} from './admin-catalog-api';
import { catalogKeys } from './use-catalog';

const adminRoot = ['admin', 'catalog'] as const;

export const adminCatalogKeys = {
  all: adminRoot,
  products: (params: AdminProductListParams) => [...adminRoot, 'products', params] as const,
  categories: () => [...adminRoot, 'categories'] as const,
};

/** Список товаров админки: статус входит в ключ, поэтому срезы кэшируются раздельно. */
export function useAdminProducts(params: AdminProductListParams) {
  return useQuery({
    queryKey: adminCatalogKeys.products(params),
    queryFn: () => fetchAdminProducts(params),
    placeholderData: keepPreviousData,
  });
}

export function useAdminCategories() {
  return useQuery({
    queryKey: adminCatalogKeys.categories(),
    queryFn: fetchAdminCategories,
  });
}

/**
 * Любая правка каталога делает прежние срезы недостижимыми: перечитываем и админский список,
 * и витрину. Redis-кэш каталога инвалидирует сам сервис, кэш TanStack Query — здесь.
 */
function invalidateCatalog(queryClient: QueryClient): void {
  void queryClient.invalidateQueries({ queryKey: adminCatalogKeys.all });
  void queryClient.invalidateQueries({ queryKey: catalogKeys.all });
}

function useCatalogMutation<TVariables, TData>(
  mutationFn: (variables: TVariables) => Promise<TData>,
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn,
    onSuccess: () => invalidateCatalog(queryClient),
  });
}

export interface UpdateProductVariables {
  id: string;
  input: ProductWriteInput;
}

export function useCreateProduct() {
  return useCatalogMutation((input: ProductWriteInput) => createProduct(input));
}

export function useUpdateProduct() {
  return useCatalogMutation(({ id, input }: UpdateProductVariables) => updateProduct(id, input));
}

export function usePublishProduct() {
  return useCatalogMutation((id: string) => publishProduct(id));
}

export function useArchiveProduct() {
  return useCatalogMutation((id: string) => archiveProduct(id));
}

export function useDeleteProduct() {
  return useCatalogMutation((id: string) => deleteProduct(id));
}

export function useCreateCategory() {
  return useCatalogMutation((input: CategoryWriteInput) => createCategory(input));
}

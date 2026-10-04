/**
 * Типы ответов API — зеркало DTO backend-сервисов.
 * JSON в camelCase (ASP.NET Core System.Text.Json по умолчанию).
 */

/** Ответ BuildingBlocks.Core.Pagination.PagedResult<T>. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

/** Catalog API · CategoryDto. */
export interface Category {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  parentId: string | null;
  sortOrder: number;
  isPublished: boolean;
}

/** Catalog API · ProductSummaryDto. */
export interface ProductSummary {
  id: string;
  name: string;
  slug: string;
  shortDescription: string | null;
  price: number;
  currency: string;
  categoryId: string;
  solventType: string | null;
  volumeLiters: number | null;
  mainImageUrl: string | null;
  status: string;
}

/** Catalog API · ProductImageDto. */
export interface ProductImage {
  id: string;
  url: string;
  altText: string | null;
  isMain: boolean;
}

/** Catalog API · ProductSpecificationDto. */
export interface ProductSpecification {
  id: string;
  name: string;
  value: string;
}

/** Catalog API · ProductDetailsDto. */
export interface ProductDetails {
  id: string;
  name: string;
  slug: string;
  shortDescription: string | null;
  description: string | null;
  price: number;
  currency: string;
  categoryId: string;
  solventType: string | null;
  volumeLiters: number | null;
  status: string;
  createdAt: string;
  updatedAt: string | null;
  images: ProductImage[];
  specifications: ProductSpecification[];
}

/** Параметры GET /api/products. */
export interface ProductListParams {
  categoryId?: string;
  solventType?: string;
  search?: string;
  page?: number;
  pageSize?: number;
  sort?: string;
}

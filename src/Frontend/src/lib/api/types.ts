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

/** Параметры GET /api/products/admin (админский срез: черновики и архив тоже). */
export interface AdminProductListParams {
  status?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

/** Тело создания/изменения товара (CreateProductCommand / UpdateProductRequest). */
export interface ProductWriteInput {
  name: string;
  categoryId: string;
  price: number;
  shortDescription?: string | null;
  description?: string | null;
  solventType?: string | null;
  volumeLiters?: number | null;
  slug?: string | null;
}

/** Тело создания категории (CreateCategoryCommand). */
export interface CategoryWriteInput {
  name: string;
  description?: string | null;
  parentId?: string | null;
  sortOrder?: number;
  slug?: string | null;
}

/** Basket API · BasketItemDto — снимок товара на момент добавления в корзину. */
export interface BasketItem {
  productId: string;
  productName: string;
  imageUrl: string | null;
  unitPrice: number;
  currency: string;
  quantity: number;
  lineTotal: number;
}

/** Basket API · BasketDto. */
export interface Basket {
  basketId: string;
  items: BasketItem[];
  totalQuantity: number;
  totalAmount: number;
  currency: string;
}

/** Order API · OrderItemDto. */
export interface OrderItem {
  productId: string;
  productName: string;
  imageUrl: string | null;
  unitPrice: number;
  currency: string;
  quantity: number;
  lineTotal: number;
}

/** Order API · OrderDto — заявка целиком. */
export interface Order {
  id: string;
  number: string;
  status: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string | null;
  comment: string | null;
  managerComment: string | null;
  preferredChannel: string;
  priority: string;
  basketId: string | null;
  items: OrderItem[];
  totalQuantity: number;
  totalAmount: number;
  currency: string;
  createdAt: string;
  updatedAt: string;
}

/** Order API · OrderSummaryDto — строка списка заявок (без позиций). */
export interface OrderSummary {
  id: string;
  number: string;
  status: string;
  customerName: string;
  customerEmail: string;
  priority: string;
  totalQuantity: number;
  totalAmount: number;
  currency: string;
  createdAt: string;
  updatedAt: string;
}

/** Тело POST /api/orders (SubmitOrderRequest): контакты вместо оплаты. */
export interface SubmitOrderInput {
  customerName: string;
  customerEmail: string;
  customerPhone?: string;
  comment?: string;
  preferredChannel?: string;
  priority?: string;
}

import { HttpResponse, http } from 'msw';
import { API_BASE_URL } from '@/lib/api/client';
import type { Category, PagedResult, ProductDetails, ProductSummary } from '@/lib/api/types';
import type { AuthSession } from '@/features/auth/types';

/** Префикс Catalog API за шлюзом — тот же, что в catalog-api.ts. */
export const catalogApiUrl = `${API_BASE_URL}/catalog/api`;

export const sampleCategories: Category[] = [
  {
    id: 'cat-1',
    name: 'Установки TD-серии',
    slug: 'td',
    description: null,
    parentId: null,
    sortOrder: 1,
    isPublished: true,
  },
  {
    id: 'cat-2',
    name: 'Комплектующие',
    slug: 'parts',
    description: null,
    parentId: null,
    sortOrder: 2,
    isPublished: true,
  },
];

export function sampleProduct(overrides: Partial<ProductSummary> = {}): ProductSummary {
  return {
    id: 'prod-1',
    name: 'Установка регенерации TD-100',
    slug: 'td-100',
    shortDescription: 'Производительность 100 л/сутки',
    price: 1250,
    currency: 'RUB',
    categoryId: 'cat-1',
    solventType: 'Хлорсодержащие',
    volumeLiters: 100,
    mainImageUrl: null,
    status: 'Published',
    ...overrides,
  };
}

export function pagedProducts(items: ProductSummary[]): PagedResult<ProductSummary> {
  return {
    items,
    page: 1,
    pageSize: 12,
    totalCount: items.length,
    totalPages: 1,
    hasNext: false,
    hasPrevious: false,
  };
}

export function sampleProductDetails(overrides: Partial<ProductDetails> = {}): ProductDetails {
  return {
    id: 'prod-1',
    name: 'Установка регенерации TD-100',
    slug: 'td-100',
    shortDescription: 'Производительность 100 л/сутки',
    description: 'Компактная установка для участка мойки.',
    price: 1250,
    currency: 'RUB',
    categoryId: 'cat-1',
    solventType: 'Хлорсодержащие',
    volumeLiters: 100,
    status: 'Published',
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: null,
    images: [{ id: 'img-1', url: 'https://cdn.techodist.local/td-100.jpg', altText: 'TD-100', isMain: true }],
    specifications: [{ id: 'spec-1', name: 'Напряжение', value: '380 В' }],
    ...overrides,
  };
}

export function sampleSession(roles: string[] = ['Admin']): AuthSession {
  return {
    tokens: { accessToken: 'access-token', refreshToken: null, expiresAt: null },
    displayName: 'Иван Петров',
    email: 'admin@techodist.local',
    roles,
  };
}

/** Обработчик списка товаров по умолчанию: один товар на странице. */
export function productsHandler(
  resolve: ProductSummary[] | ((searchParams: URLSearchParams) => ProductSummary[]) = [sampleProduct()],
) {
  return http.get(`${catalogApiUrl}/products`, ({ request }) => {
    const items = typeof resolve === 'function' ? resolve(new URL(request.url).searchParams) : resolve;

    return HttpResponse.json(pagedProducts(items));
  });
}

export function categoriesHandler() {
  return http.get(`${catalogApiUrl}/categories`, () => HttpResponse.json(sampleCategories));
}

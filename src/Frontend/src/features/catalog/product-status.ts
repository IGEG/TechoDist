/**
 * Статусы товара. Значения совпадают с Techodist.Catalog.Domain.Enums.ProductStatus —
 * backend отдаёт их строками (ProductSummaryDto.Status).
 */
export const ProductStatuses = {
  Draft: 'Draft',
  Published: 'Published',
  Archived: 'Archived',
} as const;

export type ProductStatus = (typeof ProductStatuses)[keyof typeof ProductStatuses];

/** Порядок в фильтре статусов админки. */
export const ProductStatusFilterOrder: readonly ProductStatus[] = [
  ProductStatuses.Draft,
  ProductStatuses.Published,
  ProductStatuses.Archived,
];

/** Черновик можно удалить физически, опубликованный — только снять с продажи (архив). */
export function isDeletableProductStatus(status: string): boolean {
  return status === ProductStatuses.Draft;
}

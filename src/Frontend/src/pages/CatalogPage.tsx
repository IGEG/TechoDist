import { useMemo } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { CatalogFilters, type CatalogFilterValues } from '@/features/catalog/CatalogFilters';
import { ProductCard } from '@/features/catalog/ProductCard';
import { useCategories, useProducts } from '@/features/catalog/use-catalog';
import type { ProductListParams } from '@/lib/api/types';

export const CATALOG_PAGE_SIZE = 12;

/**
 * Публичный каталог. Состояние фильтров живёт в query-строке: страница шарящаяся,
 * а данные кэшируются TanStack Query по ключу из тех же параметров.
 */
export function CatalogPage() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();

  const values = useMemo<CatalogFilterValues>(
    () => ({
      search: searchParams.get('search') ?? '',
      categoryId: searchParams.get('categoryId') ?? '',
      solventType: searchParams.get('solventType') ?? '',
    }),
    [searchParams],
  );

  const page = Math.max(Number(searchParams.get('page') ?? '1') || 1, 1);

  const params = useMemo<ProductListParams>(
    () => ({
      page,
      pageSize: CATALOG_PAGE_SIZE,
      search: values.search || undefined,
      categoryId: values.categoryId || undefined,
      solventType: values.solventType || undefined,
    }),
    [page, values],
  );

  const products = useProducts(params);
  const categories = useCategories();

  const applyFilters = (next: CatalogFilterValues) => {
    const nextParams = new URLSearchParams();

    if (next.search) nextParams.set('search', next.search);
    if (next.categoryId) nextParams.set('categoryId', next.categoryId);
    if (next.solventType) nextParams.set('solventType', next.solventType);

    // Смена фильтров всегда возвращает на первую страницу.
    setSearchParams(nextParams);
  };

  const goToPage = (nextPage: number) => {
    const nextParams = new URLSearchParams(searchParams);
    nextParams.set('page', String(nextPage));
    setSearchParams(nextParams);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const paged = products.data;

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold text-graphite-900 sm:text-3xl">{t('catalog.title')}</h1>
        <p className="max-w-3xl text-sm text-graphite-600">{t('catalog.subtitle')}</p>
      </header>

      <CatalogFilters categories={categories.data ?? []} values={values} onChange={applyFilters} />

      {products.isPending ? <p className="text-sm text-graphite-500">{t('app.loading')}</p> : null}
      {products.isError ? <Alert>{t('catalog.error')}</Alert> : null}

      {paged ? (
        <>
          <p className="text-sm text-graphite-500">
            {t('catalog.found', { total: paged.totalCount })}
          </p>

          {paged.items.length === 0 ? (
            <Alert tone="info">{t('catalog.empty')}</Alert>
          ) : (
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              {paged.items.map((product) => (
                <ProductCard key={product.id} product={product} />
              ))}
            </div>
          )}

          {paged.totalPages > 1 ? (
            <nav className="flex flex-wrap items-center justify-center gap-3">
              <Button
                variant="outline"
                size="sm"
                disabled={!paged.hasPrevious}
                onClick={() => goToPage(page - 1)}
              >
                <ChevronLeft className="h-4 w-4" />
                {t('catalog.previous')}
              </Button>
              <span className="text-sm text-graphite-600">
                {t('catalog.page', { page: paged.page, totalPages: paged.totalPages })}
              </span>
              <Button
                variant="outline"
                size="sm"
                disabled={!paged.hasNext}
                onClick={() => goToPage(page + 1)}
              >
                {t('catalog.next')}
                <ChevronRight className="h-4 w-4" />
              </Button>
            </nav>
          ) : null}
        </>
      ) : null}
    </div>
  );
}

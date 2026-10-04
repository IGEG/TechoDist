import { useMemo } from 'react';
import { Archive, Pencil, Plus, Trash2, Upload } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { buttonVariants } from '@/components/ui/button-variants';
import { Badge, Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import {
  ProductStatusFilterOrder,
  ProductStatuses,
  isDeletableProductStatus,
} from '@/features/catalog/product-status';
import {
  useAdminProducts,
  useArchiveProduct,
  useDeleteProduct,
  usePublishProduct,
} from '@/features/catalog/use-admin-catalog';
import { describeApiProblem, toApiProblem } from '@/lib/api/client';
import type { AdminProductListParams } from '@/lib/api/types';
import { cn, formatPrice } from '@/lib/utils';

export const ADMIN_PRODUCTS_PAGE_SIZE = 20;

const selectClass =
  'h-10 w-full rounded-lg border border-graphite-300 bg-white px-3 text-sm text-graphite-900 ' +
  'focus:border-accent-500 focus:ring-2 focus:ring-accent-300/60 focus:outline-none';

/**
 * Управление товарами: черновики, публикация и снятие с продажи, правка карточек.
 * Список приходит из админского среза каталога (`includeUnpublished`), поэтому черновики видны.
 */
export function AdminProductsPage() {
  const { t } = useTranslation();
  const [searchParams, setSearchParams] = useSearchParams();

  const status = searchParams.get('status') ?? '';
  const search = searchParams.get('search') ?? '';
  const page = Math.max(Number(searchParams.get('page') ?? '1') || 1, 1);

  const params = useMemo<AdminProductListParams>(
    () => ({
      page,
      pageSize: ADMIN_PRODUCTS_PAGE_SIZE,
      status: status || undefined,
      search: search || undefined,
    }),
    [page, search, status],
  );

  const products = useAdminProducts(params);
  const publish = usePublishProduct();
  const archive = useArchiveProduct();
  const remove = useDeleteProduct();

  const applyFilters = (nextStatus: string, nextSearch: string) => {
    const next = new URLSearchParams();

    if (nextStatus) next.set('status', nextStatus);
    if (nextSearch) next.set('search', nextSearch);

    // Смена фильтров всегда возвращает на первую страницу.
    setSearchParams(next);
  };

  const goToPage = (nextPage: number) => {
    const next = new URLSearchParams(searchParams);
    next.set('page', String(nextPage));
    setSearchParams(next);
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  const statusLabel = (value: string) => t(`productStatuses.${value}`, { defaultValue: value });
  const failure = publish.error ?? archive.error ?? remove.error;
  const paged = products.data;

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h1 className="text-2xl font-semibold text-graphite-900">{t('admin.products.title')}</h1>
          <p className="text-sm text-graphite-600">{t('admin.products.subtitle')}</p>
        </div>
        <Link
          to="/admin/products/new"
          className={cn(buttonVariants({ variant: 'accent' }))}
        >
          <Plus className="h-4 w-4" aria-hidden />
          {t('admin.products.create')}
        </Link>
      </header>

      <form
        className="grid gap-4 rounded-xl border border-graphite-200 bg-white p-4 md:grid-cols-[1fr_1.4fr_auto]"
        onSubmit={(event) => {
          event.preventDefault();

          const data = new FormData(event.currentTarget);

          applyFilters(
            String(data.get('status') ?? ''),
            String(data.get('search') ?? '').trim(),
          );
        }}
      >
        <div className="flex flex-col gap-1.5">
          <label className="text-xs font-medium text-graphite-500" htmlFor="admin-product-status">
            {t('admin.products.statusLabel')}
          </label>
          <select
            id="admin-product-status"
            name="status"
            key={`status-${status}`}
            defaultValue={status}
            className={selectClass}
          >
            <option value="">{t('admin.products.allStatuses')}</option>
            {ProductStatusFilterOrder.map((value) => (
              <option key={value} value={value}>
                {statusLabel(value)}
              </option>
            ))}
          </select>
        </div>

        <div className="flex flex-col gap-1.5">
          <label className="text-xs font-medium text-graphite-500" htmlFor="admin-product-search">
            {t('admin.products.searchLabel')}
          </label>
          <Input
            id="admin-product-search"
            name="search"
            key={`search-${search}`}
            defaultValue={search}
            placeholder={t('admin.products.searchPlaceholder')}
          />
        </div>

        <div className="flex items-end gap-2">
          <Button type="submit">{t('admin.products.search')}</Button>
          <Button variant="ghost" onClick={() => applyFilters('', '')}>
            {t('catalog.reset')}
          </Button>
        </div>
      </form>

      {failure ? <Alert>{describeApiProblem(toApiProblem(failure))}</Alert> : null}
      {products.isPending ? <p className="text-sm text-graphite-500">{t('app.loading')}</p> : null}
      {products.isError ? <Alert>{t('admin.products.error')}</Alert> : null}

      {paged ? (
        <>
          <p className="text-sm text-graphite-500">
            {t('catalog.found', { total: paged.totalCount })}
          </p>

          {paged.items.length === 0 ? (
            <Alert tone="info">{t('admin.products.empty')}</Alert>
          ) : (
            <Card>
              <CardContent className="overflow-x-auto p-0">
                <table className="w-full text-left text-sm">
                  <thead className="bg-graphite-50 text-xs text-graphite-500">
                    <tr>
                      <th className="px-4 py-3 font-medium">{t('admin.products.name')}</th>
                      <th className="px-4 py-3 font-medium">{t('admin.products.statusLabel')}</th>
                      <th className="px-4 py-3 font-medium">{t('admin.products.price')}</th>
                      <th className="px-4 py-3 font-medium">{t('admin.products.actions')}</th>
                    </tr>
                  </thead>
                  <tbody>
                    {paged.items.map((product) => (
                      <tr key={product.id} className="border-t border-graphite-100 align-top">
                        <td className="px-4 py-3">
                          <Link
                            to={`/catalog/${product.id}`}
                            className="font-medium text-graphite-900 hover:underline"
                          >
                            {product.name}
                          </Link>
                          <span className="block text-xs text-graphite-500">{product.slug}</span>
                        </td>
                        <td className="px-4 py-3">
                          <Badge tone={product.status === ProductStatuses.Published ? 'accent' : 'muted'}>
                            {statusLabel(product.status)}
                          </Badge>
                        </td>
                        <td className="px-4 py-3 text-graphite-600">
                          {product.price > 0
                            ? formatPrice(product.price, product.currency)
                            : t('catalog.priceOnRequest')}
                        </td>
                        <td className="px-4 py-3">
                          <div className="flex flex-wrap items-center gap-2">
                            <Link
                              to={`/admin/products/${product.id}/edit`}
                              className={cn(buttonVariants({ variant: 'outline', size: 'sm' }))}
                            >
                              <Pencil className="h-4 w-4" aria-hidden />
                              {t('admin.products.edit')}
                            </Link>

                            {product.status !== ProductStatuses.Published ? (
                              <Button
                                variant="accent"
                                size="sm"
                                disabled={publish.isPending && publish.variables === product.id}
                                onClick={() => publish.mutate(product.id)}
                              >
                                <Upload className="h-4 w-4" aria-hidden />
                                {t('admin.products.publish')}
                              </Button>
                            ) : (
                              <Button
                                variant="outline"
                                size="sm"
                                disabled={archive.isPending && archive.variables === product.id}
                                onClick={() => archive.mutate(product.id)}
                              >
                                <Archive className="h-4 w-4" aria-hidden />
                                {t('admin.products.archive')}
                              </Button>
                            )}

                            {isDeletableProductStatus(product.status) ? (
                              <Button
                                variant="ghost"
                                size="sm"
                                aria-label={`${t('admin.products.delete')}: ${product.name}`}
                                disabled={remove.isPending && remove.variables === product.id}
                                onClick={() => remove.mutate(product.id)}
                              >
                                <Trash2 className="h-4 w-4" aria-hidden />
                              </Button>
                            ) : null}
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </CardContent>
            </Card>
          )}

          {paged.totalPages > 1 ? (
            <nav className="flex flex-wrap items-center justify-center gap-3">
              <Button
                variant="outline"
                size="sm"
                disabled={!paged.hasPrevious}
                onClick={() => goToPage(page - 1)}
              >
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
              </Button>
            </nav>
          ) : null}
        </>
      ) : null}
    </div>
  );
}

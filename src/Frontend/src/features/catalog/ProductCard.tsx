import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { buttonVariants } from '@/components/ui/button-variants';
import { Badge } from '@/components/ui/card';
import type { ProductSummary } from '@/lib/api/types';
import { cn, formatPrice } from '@/lib/utils';

interface ProductCardProps {
  product: ProductSummary;
}

/** Карточка каталога: основное фото, тип растворителя, объём и цена. */
export function ProductCard({ product }: ProductCardProps) {
  const { t } = useTranslation();
  const hasPrice = product.price > 0;

  return (
    <article className="flex h-full flex-col overflow-hidden rounded-xl border border-graphite-200 bg-white shadow-sm transition hover:shadow-md">
      <div className="grid h-40 place-items-center bg-graphite-100 text-sm text-graphite-400">
        {product.mainImageUrl ? (
          <img
            src={product.mainImageUrl}
            alt={product.name}
            loading="lazy"
            className="h-full w-full object-cover"
          />
        ) : (
          t('catalog.noPhoto')
        )}
      </div>

      <div className="flex flex-1 flex-col gap-2 p-4">
        <div className="flex flex-wrap gap-2">
          {product.solventType ? <Badge tone="muted">{product.solventType}</Badge> : null}
          {product.volumeLiters ? (
            <Badge tone="accent">{t('catalog.volume', { liters: product.volumeLiters })}</Badge>
          ) : null}
        </div>

        <h3 className="text-base font-semibold text-graphite-900">{product.name}</h3>

        {product.shortDescription ? (
          <p className="line-clamp-2 text-sm text-graphite-600">{product.shortDescription}</p>
        ) : null}

        <div className="mt-auto flex items-center justify-between gap-3 pt-2">
          <span className="text-base font-semibold text-graphite-900">
            {hasPrice ? formatPrice(product.price, product.currency) : t('catalog.priceOnRequest')}
          </span>
          <Link
            to={`/catalog/${product.id}`}
            className={cn(buttonVariants({ variant: 'outline', size: 'sm' }))}
          >
            {t('catalog.details')}
          </Link>
        </div>
      </div>
    </article>
  );
}

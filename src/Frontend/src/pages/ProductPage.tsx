import { ArrowLeft } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link, useParams } from 'react-router';
import { Alert } from '@/components/ui/alert';
import { buttonVariants } from '@/components/ui/button-variants';
import { Badge, Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { AddToBasketButton } from '@/features/basket/AddToBasketButton';
import { useProduct } from '@/features/catalog/use-catalog';
import { cn, formatPrice } from '@/lib/utils';

/** Карточка товара: описание, изображения и характеристики установки. */
export function ProductPage() {
  const { t } = useTranslation();
  const { productId } = useParams<{ productId: string }>();
  const product = useProduct(productId);

  if (product.isPending) {
    return <p className="text-sm text-graphite-500">{t('app.loading')}</p>;
  }

  if (product.isError || !product.data) {
    return (
      <div className="flex flex-col gap-4">
        <Alert>{t('product.error')}</Alert>
        <Link to="/catalog" className={cn(buttonVariants({ variant: 'outline' }), 'self-start')}>
          <ArrowLeft className="h-4 w-4" />
          {t('product.back')}
        </Link>
      </div>
    );
  }

  const details = product.data;
  const images = details.images.length > 0 ? details.images : [];

  return (
    <div className="flex flex-col gap-6">
      <Link
        to="/catalog"
        className={cn(buttonVariants({ variant: 'ghost', size: 'sm' }), 'self-start')}
      >
        <ArrowLeft className="h-4 w-4" />
        {t('product.back')}
      </Link>

      <div className="grid gap-8 lg:grid-cols-2">
        <div className="flex flex-col gap-3">
          <div className="grid aspect-4/3 place-items-center overflow-hidden rounded-xl border border-graphite-200 bg-graphite-100 text-sm text-graphite-400">
            {images.length > 0 ? (
              <img
                src={(images.find((image) => image.isMain) ?? images[0]).url}
                alt={details.name}
                className="h-full w-full object-cover"
              />
            ) : (
              t('catalog.noPhoto')
            )}
          </div>

          {images.length > 1 ? (
            <div className="grid grid-cols-4 gap-2">
              {images.map((image) => (
                <img
                  key={image.id}
                  src={image.url}
                  alt={image.altText ?? details.name}
                  loading="lazy"
                  className="aspect-square w-full rounded-lg border border-graphite-200 object-cover"
                />
              ))}
            </div>
          ) : null}
        </div>

        <div className="flex flex-col gap-4">
          <div className="flex flex-wrap gap-2">
            {details.solventType ? <Badge tone="muted">{details.solventType}</Badge> : null}
            {details.volumeLiters ? (
              <Badge tone="accent">{t('catalog.volume', { liters: details.volumeLiters })}</Badge>
            ) : null}
          </div>

          <h1 className="text-2xl font-semibold text-graphite-900 sm:text-3xl">{details.name}</h1>

          {details.shortDescription ? (
            <p className="text-sm text-graphite-600">{details.shortDescription}</p>
          ) : null}

          <p className="text-2xl font-semibold text-graphite-900">
            {details.price > 0
              ? formatPrice(details.price, details.currency)
              : t('product.priceOnRequest')}
          </p>

          <AddToBasketButton productId={details.id} />

          {details.description ? (
            <Card>
              <CardHeader>
                <CardTitle>{t('product.description')}</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="whitespace-pre-line text-sm text-graphite-600">{details.description}</p>
              </CardContent>
            </Card>
          ) : null}

          {details.specifications.length > 0 ? (
            <Card>
              <CardHeader>
                <CardTitle>{t('product.specifications')}</CardTitle>
              </CardHeader>
              <CardContent>
                <dl className="grid gap-2 text-sm">
                  {details.specifications.map((specification) => (
                    <div
                      key={specification.id}
                      className="flex justify-between gap-4 border-b border-graphite-100 pb-2 last:border-0"
                    >
                      <dt className="text-graphite-500">{specification.name}</dt>
                      <dd className="text-right font-medium text-graphite-800">
                        {specification.value}
                      </dd>
                    </div>
                  ))}
                </dl>
              </CardContent>
            </Card>
          ) : null}
        </div>
      </div>
    </div>
  );
}

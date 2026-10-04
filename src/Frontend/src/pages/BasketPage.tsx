import { Minus, Plus, Trash2 } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { buttonVariants } from '@/components/ui/button-variants';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { MAX_BASKET_ITEM_QUANTITY } from '@/features/basket/basket-api';
import {
  useBasket,
  useClearBasket,
  useRemoveBasketItem,
  useUpdateBasketItemQuantity,
} from '@/features/basket/use-basket';
import { cn, formatPrice } from '@/lib/utils';

/**
 * Гостевая корзина. Состав хранится на сервере (Redis + анонимный cookie, ADR 0005),
 * поэтому страница — только представление: цена и название приходят снимком из Basket.
 */
export function BasketPage() {
  const { t } = useTranslation();
  const basket = useBasket();
  const updateQuantity = useUpdateBasketItemQuantity();
  const removeItem = useRemoveBasketItem();
  const clear = useClearBasket();

  if (basket.isPending) {
    return <p className="text-sm text-graphite-500">{t('app.loading')}</p>;
  }

  if (basket.isError || !basket.data) {
    return <Alert>{t('cart.error')}</Alert>;
  }

  const data = basket.data;

  if (data.items.length === 0) {
    return (
      <div className="flex flex-col gap-4">
        <h1 className="text-2xl font-semibold text-graphite-900">{t('cart.title')}</h1>
        <Alert tone="info">{t('cart.empty')}</Alert>
        <Link to="/catalog" className={cn(buttonVariants({ variant: 'primary' }), 'self-start')}>
          {t('cart.toCatalog')}
        </Link>
      </div>
    );
  }

  const mutationFailed = updateQuantity.isError || removeItem.isError || clear.isError;

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h1 className="text-2xl font-semibold text-graphite-900">{t('cart.title')}</h1>
          <p className="text-sm text-graphite-600">{t('cart.subtitle')}</p>
        </div>
        <Button variant="ghost" disabled={clear.isPending} onClick={() => clear.mutate()}>
          <Trash2 className="h-4 w-4" aria-hidden />
          {t('cart.clear')}
        </Button>
      </header>

      <Card>
        <CardHeader>
          <CardTitle>{t('cart.items', { count: data.totalQuantity })}</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          {data.items.map((item) => (
            <div
              key={item.productId}
              className="flex flex-wrap items-center gap-4 border-b border-graphite-100 pb-4 last:border-0 last:pb-0"
            >
              <div className="grid h-16 w-16 shrink-0 place-items-center overflow-hidden rounded-lg bg-graphite-100 text-xs text-graphite-400">
                {item.imageUrl ? (
                  <img
                    src={item.imageUrl}
                    alt={item.productName}
                    loading="lazy"
                    className="h-full w-full object-cover"
                  />
                ) : (
                  t('catalog.noPhoto')
                )}
              </div>

              <div className="flex min-w-48 flex-1 flex-col gap-1">
                <Link
                  to={`/catalog/${item.productId}`}
                  className="text-sm font-semibold text-graphite-900 hover:underline"
                >
                  {item.productName}
                </Link>
                <span className="text-sm text-graphite-500">
                  {formatPrice(item.unitPrice, item.currency)}
                </span>
              </div>

              <div className="flex items-center gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  aria-label={t('cart.decrease')}
                  disabled={item.quantity <= 1 || updateQuantity.isPending}
                  onClick={() =>
                    updateQuantity.mutate({ productId: item.productId, quantity: item.quantity - 1 })
                  }
                >
                  <Minus className="h-4 w-4" aria-hidden />
                </Button>
                <span className="min-w-8 text-center text-sm font-medium text-graphite-800">
                  {item.quantity}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  aria-label={t('cart.increase')}
                  disabled={item.quantity >= MAX_BASKET_ITEM_QUANTITY || updateQuantity.isPending}
                  onClick={() =>
                    updateQuantity.mutate({ productId: item.productId, quantity: item.quantity + 1 })
                  }
                >
                  <Plus className="h-4 w-4" aria-hidden />
                </Button>
              </div>

              <span className="min-w-24 text-right text-sm font-semibold text-graphite-900">
                {formatPrice(item.lineTotal, item.currency)}
              </span>

              <Button
                variant="ghost"
                size="sm"
                aria-label={`${t('cart.remove')}: ${item.productName}`}
                disabled={removeItem.isPending}
                onClick={() => removeItem.mutate(item.productId)}
              >
                <Trash2 className="h-4 w-4" aria-hidden />
              </Button>
            </div>
          ))}


        </CardContent>
      </Card>

      {mutationFailed ? <Alert>{t('cart.error')}</Alert> : null}

      <div className="flex flex-wrap items-center justify-between gap-4">
        <p className="text-lg font-semibold text-graphite-900">
          {t('cart.total', { total: formatPrice(data.totalAmount, data.currency) })}
        </p>
        <Link to="/checkout" className={cn(buttonVariants({ variant: 'accent', size: 'lg' }))}>
          {t('cart.checkout')}
        </Link>
      </div>
    </div>
  );
}

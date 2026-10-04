import type { FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { buttonVariants } from '@/components/ui/button-variants';
import { Badge, Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { OrderStatusSequence, OrderStatuses } from '@/features/orders/order-status';
import { useOrderByNumber } from '@/features/orders/use-orders';
import { toApiProblem } from '@/lib/api/client';
import { cn, formatPrice } from '@/lib/utils';

/** Дата заявки в читаемом виде: «04.10.2026, 18:30». */
function formatMoment(value: string): string {
  return new Intl.DateTimeFormat('ru-RU', { dateStyle: 'short', timeStyle: 'short' }).format(
    new Date(value),
  );
}

/**
 * Статус заявки по номеру из письма. Страница публичная: гость не аутентифицируется,
 * а номер знает только он (ADR 0005) — внутренний GUID наружу не отдаём.
 */
export function OrderStatusPage() {
  const { t } = useTranslation();
  const { number } = useParams<{ number?: string }>();
  const navigate = useNavigate();
  const order = useOrderByNumber(number);

  const lookup = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const value = String(new FormData(event.currentTarget).get('number') ?? '').trim();

    if (value) {
      navigate(`/orders/${encodeURIComponent(value)}`);
    }
  };

  if (!number) {
    return (
      <div className="flex flex-col gap-6">
        <header className="flex flex-col gap-1">
          <h1 className="text-2xl font-semibold text-graphite-900">{t('orderStatus.title')}</h1>
          <p className="text-sm text-graphite-600">{t('orderStatus.subtitle')}</p>
        </header>

        <Card className="max-w-xl">
          <CardContent>
            <form className="flex flex-col gap-4" onSubmit={lookup}>
              <div className="flex flex-col gap-1.5">
                <label className="text-sm font-medium text-graphite-600" htmlFor="order-number">
                  {t('orderStatus.numberLabel')}
                </label>
                <Input
                  id="order-number"
                  name="number"
                  placeholder={t('orderStatus.numberPlaceholder')}
                />
              </div>

              <Button type="submit" className="self-start">
                {t('orderStatus.lookup')}
              </Button>
            </form>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (order.isPending) {
    return <p className="text-sm text-graphite-500">{t('app.loading')}</p>;
  }

  if (order.isError || !order.data) {
    return (
      <div className="flex flex-col gap-4">
        <h1 className="text-2xl font-semibold text-graphite-900">{t('orderStatus.title')}</h1>
        <Alert>
          {toApiProblem(order.error).status === 404
            ? t('orderStatus.notFound')
            : t('orderStatus.error')}
        </Alert>
        <Link to="/orders" className={cn(buttonVariants({ variant: 'outline' }), 'self-start')}>
          {t('orderStatus.lookupAnother')}
        </Link>
      </div>
    );
  }

  const data = order.data;
  const cancelled = data.status === OrderStatuses.Cancelled;
  // Докуда дошла заявка: индекс текущего статуса в воронке (−1, если статус вне воронки).
  const reachedIndex = OrderStatusSequence.findIndex((status) => status === data.status);

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h1 className="text-2xl font-semibold text-graphite-900">{t('orderStatus.title')}</h1>
          <p className="text-sm text-graphite-600">{t('orderStatus.subtitle')}</p>
        </div>
        <Link to="/orders" className={cn(buttonVariants({ variant: 'ghost', size: 'sm' }))}>
          {t('orderStatus.lookupAnother')}
        </Link>
      </header>

      <Card>
        <CardHeader className="flex flex-wrap items-center justify-between gap-3">
          <CardTitle>{data.number}</CardTitle>
          <Badge tone="accent">{t(`orderStatuses.${data.status}`, { defaultValue: data.status })}</Badge>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-wrap gap-2">
            {cancelled ? null : (
              OrderStatusSequence.map((status, index) => (
                <Badge key={status} tone={index <= reachedIndex ? 'accent' : 'muted'}>
                  {t(`orderStatuses.${status}`, { defaultValue: status })}
                </Badge>
              ))
            )}
          </div>

          <p className="text-sm text-graphite-500">
            {t('orderStatus.created')}: {formatMoment(data.createdAt)}
          </p>
        </CardContent>
      </Card>

      <div className="grid gap-6 lg:grid-cols-[1.4fr_1fr]">
        <Card>
          <CardHeader>
            <CardTitle>{t('orderStatus.items')}</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-3">
            {data.items.map((item) => (
              <div key={item.productId} className="flex justify-between gap-4 text-sm">
                <span className="text-graphite-600">
                  {item.productName} × {item.quantity}
                </span>
                <span className="font-medium text-graphite-900">
                  {formatPrice(item.lineTotal, item.currency)}
                </span>
              </div>
            ))}
            <p className="border-t border-graphite-100 pt-3 text-base font-semibold text-graphite-900">
              {t('cart.total', { total: formatPrice(data.totalAmount, data.currency) })}
            </p>
          </CardContent>
        </Card>

        <Card className="self-start">
          <CardHeader>
            <CardTitle>{t('orderStatus.customer')}</CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-2 text-sm text-graphite-600">
            <span className="font-medium text-graphite-900">{data.customerName}</span>
            <span>{data.customerEmail}</span>
            {data.customerPhone ? <span>{data.customerPhone}</span> : null}
            {data.comment ? <span className="whitespace-pre-line">{data.comment}</span> : null}
            {data.managerComment ? (
              <span className="whitespace-pre-line text-graphite-500">{data.managerComment}</span>
            ) : null}
            <Link
              to="/catalog"
              className={cn(buttonVariants({ variant: 'outline', size: 'sm' }), 'self-start')}
            >
              {t('cart.toCatalog')}
            </Link>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}

import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router';
import { z } from 'zod';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { buttonVariants } from '@/components/ui/button-variants';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { useBasket } from '@/features/basket/use-basket';
import {
  OrderContactChannels,
  OrderPriorities,
} from '@/features/orders/order-status';
import { useSubmitOrder } from '@/features/orders/use-orders';
import { describeApiProblem, toApiProblem } from '@/lib/api/client';
import { cn, formatPrice } from '@/lib/utils';

const checkoutSchema = z.object({
  customerName: z.string().trim().min(1, 'Укажите имя').max(200, 'Слишком длинное имя'),
  customerEmail: z.string().trim().min(1, 'Укажите e-mail').email('Некорректный e-mail'),
  customerPhone: z.string().trim().max(50, 'Слишком длинный номер').optional(),
  comment: z.string().trim().max(2000, 'Слишком длинный комментарий').optional(),
  preferredChannel: z.enum([OrderContactChannels.Email, OrderContactChannels.Phone]),
  priority: z.enum([OrderPriorities.Standard, OrderPriorities.Urgent]),
});

type CheckoutFormValues = z.infer<typeof checkoutSchema>;

const fieldClass = 'text-sm font-medium text-graphite-600';

/**
 * Оформление заявки (онлайн-оплаты нет). Контакты и комментарий уходят в Order API,
 * позиции сервис берёт из гостевой корзины по cookie; после успеха показываем номер заявки.
 */
export function CheckoutPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const basket = useBasket();
  const submitOrder = useSubmitOrder();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CheckoutFormValues>({
    resolver: zodResolver(checkoutSchema),
    defaultValues: {
      customerName: '',
      customerEmail: '',
      customerPhone: '',
      comment: '',
      preferredChannel: OrderContactChannels.Email,
      priority: OrderPriorities.Standard,
    },
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      const order = await submitOrder.mutateAsync({
        customerName: values.customerName,
        customerEmail: values.customerEmail,
        customerPhone: values.customerPhone || undefined,
        comment: values.comment || undefined,
        preferredChannel: values.preferredChannel,
        priority: values.priority,
      });

      navigate(`/orders/${encodeURIComponent(order.number)}`, { replace: true });
    } catch {
      // Текст ошибки показывает submitOrder.error (ProblemDetails от Order API).
    }
  });

  const fieldError = (message: string | undefined) =>
    message ? <p className="text-xs text-red-600">{message}</p> : null;

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
        <h1 className="text-2xl font-semibold text-graphite-900">{t('checkout.title')}</h1>
        <Alert tone="info">{t('checkout.emptyBasket')}</Alert>
        <Link to="/catalog" className={cn(buttonVariants({ variant: 'primary' }), 'self-start')}>
          {t('cart.toCatalog')}
        </Link>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold text-graphite-900">{t('checkout.title')}</h1>
        <p className="max-w-3xl text-sm text-graphite-600">{t('checkout.subtitle')}</p>
      </header>

      <div className="grid gap-6 lg:grid-cols-[1.4fr_1fr]">
        <Card>
          <CardContent>
            <form className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
              <div className="flex flex-col gap-1.5">
                <label className={fieldClass} htmlFor="checkout-name">
                  {t('checkout.name')}
                </label>
                <Input
                  id="checkout-name"
                  autoComplete="name"
                  aria-invalid={errors.customerName ? true : undefined}
                  {...register('customerName')}
                />
                {fieldError(errors.customerName?.message)}
              </div>

              <div className="flex flex-col gap-1.5">
                <label className={fieldClass} htmlFor="checkout-email">
                  {t('checkout.email')}
                </label>
                <Input
                  id="checkout-email"
                  type="email"
                  autoComplete="email"
                  aria-invalid={errors.customerEmail ? true : undefined}
                  {...register('customerEmail')}
                />
                {fieldError(errors.customerEmail?.message)}
              </div>

              <div className="flex flex-col gap-1.5">
                <label className={fieldClass} htmlFor="checkout-phone">
                  {t('checkout.phone')}
                </label>
                <Input
                  id="checkout-phone"
                  type="tel"
                  autoComplete="tel"
                  aria-invalid={errors.customerPhone ? true : undefined}
                  {...register('customerPhone')}
                />
                {fieldError(errors.customerPhone?.message)}
              </div>

              <div className="grid gap-4 sm:grid-cols-2">
                <div className="flex flex-col gap-1.5">
                  <label className={fieldClass} htmlFor="checkout-channel">
                    {t('checkout.channel')}
                  </label>
                  <select
                    id="checkout-channel"
                    className="h-10 w-full rounded-lg border border-graphite-300 bg-white px-3 text-sm text-graphite-900 focus:border-accent-500 focus:ring-2 focus:ring-accent-300/60 focus:outline-none"
                    {...register('preferredChannel')}
                  >
                    <option value={OrderContactChannels.Email}>{t('checkout.channelEmail')}</option>
                    <option value={OrderContactChannels.Phone}>{t('checkout.channelPhone')}</option>
                  </select>
                </div>

                <div className="flex flex-col gap-1.5">
                  <label className={fieldClass} htmlFor="checkout-priority">
                    {t('checkout.priority')}
                  </label>
                  <select
                    id="checkout-priority"
                    className="h-10 w-full rounded-lg border border-graphite-300 bg-white px-3 text-sm text-graphite-900 focus:border-accent-500 focus:ring-2 focus:ring-accent-300/60 focus:outline-none"
                    {...register('priority')}
                  >
                    <option value={OrderPriorities.Standard}>{t('checkout.priorityStandard')}</option>
                    <option value={OrderPriorities.Urgent}>{t('checkout.priorityUrgent')}</option>
                  </select>
                </div>
              </div>

              <div className="flex flex-col gap-1.5">
                <label className={fieldClass} htmlFor="checkout-comment">
                  {t('checkout.comment')}
                </label>
                <Textarea
                  id="checkout-comment"
                  rows={4}
                  aria-invalid={errors.comment ? true : undefined}
                  {...register('comment')}
                />
                {fieldError(errors.comment?.message)}
              </div>

              {submitOrder.isError ? (
                <Alert>{describeApiProblem(toApiProblem(submitOrder.error))}</Alert>
              ) : null}

              <Button type="submit" size="lg" disabled={submitOrder.isPending}>
                {submitOrder.isPending ? t('checkout.submitting') : t('checkout.submit')}
              </Button>
            </form>
          </CardContent>
        </Card>

        <Card className="self-start">
          <CardHeader>
            <CardTitle>{t('checkout.summaryTitle')}</CardTitle>
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
      </div>
    </div>
  );
}


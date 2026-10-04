import { ShoppingCart } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router';
import { cn } from '@/lib/utils';
import { useBasket } from './use-basket';

/**
 * Ссылка на корзину с бейджем количества. Количество берётся из той же кэшируемой корзины,
 * что и на странице корзины, поэтому бейдж обновляется сразу после добавления товара.
 */
export function BasketLink() {
  const { t } = useTranslation();
  const basket = useBasket();
  const quantity = basket.data?.totalQuantity ?? 0;

  return (
    <NavLink
      to="/cart"
      className={({ isActive }) =>
        cn(
          'inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
          isActive ? 'bg-graphite-100 text-graphite-900' : 'text-graphite-600 hover:bg-graphite-50',
        )
      }
      data-testid="basket-link"
    >
      <ShoppingCart className="h-4 w-4" aria-hidden />
      {t('nav.cart')}
      {quantity > 0 ? (
        <span className="inline-flex min-w-5 items-center justify-center rounded-full bg-accent-500 px-1.5 py-0.5 text-xs font-semibold text-graphite-900">
          {quantity}
        </span>
      ) : null}
    </NavLink>
  );
}

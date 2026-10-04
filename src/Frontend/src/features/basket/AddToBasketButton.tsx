import { ShoppingCart } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Alert } from '@/components/ui/alert';
import { Button, type ButtonProps } from '@/components/ui/button';
import { toApiProblem } from '@/lib/api/client';
import { useAddBasketItem } from './use-basket';

interface AddToBasketButtonProps {
  productId: string;
  size?: ButtonProps['size'];
}

/**
 * Добавление товара в гостевую корзину. Идентификатор берётся статусом 200/пустой корзиной;
 * снимок названия и цены сервис Basket делает сам из Catalog (ADR 0005).
 */
export function AddToBasketButton({ productId, size = 'lg' }: AddToBasketButtonProps) {
  const { t } = useTranslation();
  const addItem = useAddBasketItem();

  return (
    <div className="flex flex-col gap-2">
      <Button
        size={size}
        disabled={addItem.isPending}
        onClick={() => addItem.mutate({ productId })}
      >
        <ShoppingCart className="h-4 w-4" aria-hidden />
        {t('cart.add')}
      </Button>

      {addItem.isSuccess ? (
        <p role="status" className="text-sm text-graphite-600">
          {t('cart.added')}
        </p>
      ) : null}

      {addItem.isError ? <Alert>{toApiProblem(addItem.error).detail ?? t('cart.addError')}</Alert> : null}
    </div>
  );
}

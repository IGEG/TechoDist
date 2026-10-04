import { useEffect } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useParams } from 'react-router';
import { z } from 'zod';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { buttonVariants } from '@/components/ui/button-variants';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { useAdminCategories, useCreateProduct, useUpdateProduct } from '@/features/catalog/use-admin-catalog';
import { useProduct } from '@/features/catalog/use-catalog';
import { describeApiProblem, toApiProblem } from '@/lib/api/client';
import type { ProductDetails, ProductWriteInput } from '@/lib/api/types';
import { cn } from '@/lib/utils';

/**
 * Форма товара. Все поля — строки (числа проверяются регуляркой и приводятся при отправке):
 * так форма одинаково читает и пустой черновик, и существующую карточку.
 */
const productFormSchema = z.object({
  name: z.string().trim().min(2, 'Укажите название товара'),
  categoryId: z.string().min(1, 'Выберите категорию товара'),
  price: z.string().trim().regex(/^\d+(?:[.,]\d{1,2})?$/, 'Цена — число, например 1250'),
  slug: z
    .string()
    .trim()
    .regex(/^$|^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Slug — латиница и дефисы, например td-100'),
  shortDescription: z.string().trim().max(500, 'Не больше 500 символов'),
  description: z.string().trim(),
  solventType: z.string().trim(),
  volumeLiters: z.string().trim().regex(/^\d*$/, 'Объём — целое число литров'),
});

type ProductFormValues = z.infer<typeof productFormSchema>;

const emptyForm: ProductFormValues = {
  name: '',
  categoryId: '',
  price: '0',
  slug: '',
  shortDescription: '',
  description: '',
  solventType: '',
  volumeLiters: '',
};

const fieldClass = 'text-sm font-medium text-graphite-600';
const selectClass =
  'h-10 w-full rounded-lg border border-graphite-300 bg-white px-3 text-sm text-graphite-900 ' +
  'focus:border-accent-500 focus:ring-2 focus:ring-accent-300/60 focus:outline-none';

/** Карточка товара → значения формы. */
function toFormValues(product: ProductDetails): ProductFormValues {
  return {
    name: product.name,
    categoryId: product.categoryId,
    price: String(product.price),
    slug: product.slug,
    shortDescription: product.shortDescription ?? '',
    description: product.description ?? '',
    solventType: product.solventType ?? '',
    volumeLiters: product.volumeLiters ? String(product.volumeLiters) : '',
  };
}

/** Значения формы → тело запроса Catalog API (пустые строки уходят как null). */
function toWriteInput(values: ProductFormValues): ProductWriteInput {
  return {
    name: values.name,
    categoryId: values.categoryId,
    price: Number(values.price.replace(',', '.')),
    slug: values.slug || null,
    shortDescription: values.shortDescription || null,
    description: values.description || null,
    solventType: values.solventType || null,
    volumeLiters: values.volumeLiters ? Number(values.volumeLiters) : null,
  };
}

/**
 * Создание и правка товара. Новый товар сохраняется черновиком — публикация отдельным
 * действием в списке (так менеджер успевает заполнить характеристики и цену).
 */
export function AdminProductFormPage() {
  const { t } = useTranslation();
  const { productId } = useParams<{ productId?: string }>();
  const navigate = useNavigate();
  const isEdit = Boolean(productId);

  const categories = useAdminCategories();
  const product = useProduct(productId);
  const createProduct = useCreateProduct();
  const updateProduct = useUpdateProduct();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<ProductFormValues>({
    resolver: zodResolver(productFormSchema),
    defaultValues: emptyForm,
  });

  // Правка: карточка приходит из Catalog — заполняем форму, когда данные готовы.
  useEffect(() => {
    if (product.data) {
      reset(toFormValues(product.data));
    }
  }, [product.data, reset]);

  const onSubmit = handleSubmit(async (values) => {
    const input = toWriteInput(values);

    try {
      if (productId) {
        await updateProduct.mutateAsync({ id: productId, input });
      } else {
        await createProduct.mutateAsync(input);
      }

      navigate('/admin/products', { replace: true });
    } catch {
      // Текст ошибки показывает активная мутация (createProduct/updateProduct).
    }
  });

  const failure = createProduct.error ?? updateProduct.error;
  const isPending = createProduct.isPending || updateProduct.isPending;

  const fieldError = (message: string | undefined) =>
    message ? <p className="text-xs text-red-600">{message}</p> : null;

  if (isEdit && product.isPending) {
    return <p className="text-sm text-graphite-500">{t('app.loading')}</p>;
  }

  if (isEdit && (product.isError || !product.data)) {
    return (
      <div className="flex flex-col gap-4">
        <Alert>{t('product.error')}</Alert>
        <Link
          to="/admin/products"
          className={cn(buttonVariants({ variant: 'outline' }), 'self-start')}
        >
          {t('admin.products.back')}
        </Link>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold text-graphite-900">
          {isEdit ? t('admin.products.editTitle') : t('admin.products.createTitle')}
        </h1>
        <p className="text-sm text-graphite-600">{t('admin.products.formSubtitle')}</p>
      </header>

      {categories.data && categories.data.length === 0 ? (
        <Alert tone="info">
          {t('admin.products.categoryRequired')}{' '}
          <Link to="/admin/categories" className="underline">
            {t('admin.categories.create')}
          </Link>
        </Alert>
      ) : null}

      <Card>
        <CardContent>
          <form className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
            <div className="flex flex-col gap-1.5">
              <label className={fieldClass} htmlFor="product-name">
                {t('admin.products.name')}
              </label>
              <Input
                id="product-name"
                aria-invalid={errors.name ? true : undefined}
                {...register('name')}
              />
              {fieldError(errors.name?.message)}
            </div>

            <div className="flex flex-col gap-1.5">
              <label className={fieldClass} htmlFor="product-category">
                {t('admin.products.category')}
              </label>
              <select
                id="product-category"
                className={selectClass}
                aria-invalid={errors.categoryId ? true : undefined}
                {...register('categoryId')}
              >
                <option value="">{t('admin.products.categoryPlaceholder')}</option>
                {(categories.data ?? []).map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
              {fieldError(errors.categoryId?.message)}
            </div>

            <div className="grid gap-4 sm:grid-cols-3">
              <div className="flex flex-col gap-1.5">
                <label className={fieldClass} htmlFor="product-price">
                  {t('admin.products.price')}
                </label>
                <Input
                  id="product-price"
                  inputMode="decimal"
                  aria-invalid={errors.price ? true : undefined}
                  {...register('price')}
                />
                {fieldError(errors.price?.message)}
              </div>

              <div className="flex flex-col gap-1.5">
                <label className={fieldClass} htmlFor="product-volume">
                  {t('admin.products.volumeLiters')}
                </label>
                <Input
                  id="product-volume"
                  inputMode="numeric"
                  aria-invalid={errors.volumeLiters ? true : undefined}
                  {...register('volumeLiters')}
                />
                {fieldError(errors.volumeLiters?.message)}
              </div>

              <div className="flex flex-col gap-1.5">
                <label className={fieldClass} htmlFor="product-solvent">
                  {t('admin.products.solventType')}
                </label>
                <Input id="product-solvent" {...register('solventType')} />
              </div>
            </div>

            <div className="flex flex-col gap-1.5">
              <label className={fieldClass} htmlFor="product-slug">
                {t('admin.products.slug')}
              </label>
              <Input
                id="product-slug"
                aria-invalid={errors.slug ? true : undefined}
                {...register('slug')}
              />
              <p className="text-xs text-graphite-500">{t('admin.products.slugHint')}</p>
              {fieldError(errors.slug?.message)}
            </div>

            <div className="flex flex-col gap-1.5">
              <label className={fieldClass} htmlFor="product-short-description">
                {t('admin.products.shortDescription')}
              </label>
              <Input
                id="product-short-description"
                aria-invalid={errors.shortDescription ? true : undefined}
                {...register('shortDescription')}
              />
              {fieldError(errors.shortDescription?.message)}
            </div>

            <div className="flex flex-col gap-1.5">
              <label className={fieldClass} htmlFor="product-description">
                {t('admin.products.description')}
              </label>
              <Textarea id="product-description" rows={6} {...register('description')} />
            </div>

            {failure ? <Alert>{describeApiProblem(toApiProblem(failure))}</Alert> : null}

            <div className="flex flex-wrap items-center gap-3">
              <Button type="submit" size="lg" disabled={isPending}>
                {isPending ? t('admin.products.saving') : t('admin.products.save')}
              </Button>
              <Link
                to="/admin/products"
                className={cn(buttonVariants({ variant: 'ghost', size: 'lg' }))}
              >
                {t('admin.products.back')}
              </Link>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

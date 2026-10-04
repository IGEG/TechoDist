import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { z } from 'zod';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Badge, Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { useAdminCategories, useCreateCategory } from '@/features/catalog/use-admin-catalog';
import { describeApiProblem, toApiProblem } from '@/lib/api/client';

const categoryFormSchema = z.object({
  name: z.string().trim().min(2, 'Укажите название категории'),
  slug: z
    .string()
    .trim()
    .regex(/^$|^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Slug — латиница и дефисы, например td-units'),
  sortOrder: z.string().trim().regex(/^\d*$/, 'Порядок — целое число'),
  description: z.string().trim(),
});

type CategoryFormValues = z.infer<typeof categoryFormSchema>;

const emptyForm: CategoryFormValues = { name: '', slug: '', sortOrder: '0', description: '' };

const fieldClass = 'text-sm font-medium text-graphite-600';

/** Категории каталога: список (включая неопубликованные) и создание новой. */
export function AdminCategoriesPage() {
  const { t } = useTranslation();
  const categories = useAdminCategories();
  const createCategory = useCreateCategory();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<CategoryFormValues>({
    resolver: zodResolver(categoryFormSchema),
    defaultValues: emptyForm,
  });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await createCategory.mutateAsync({
        name: values.name,
        slug: values.slug || null,
        sortOrder: values.sortOrder ? Number(values.sortOrder) : 0,
        description: values.description || null,
      });

      reset(emptyForm);
    } catch {
      // Текст ошибки показывает createCategory.error (например, дубль названия — 409).
    }
  });

  const fieldError = (message: string | undefined) =>
    message ? <p className="text-xs text-red-600">{message}</p> : null;

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-col gap-1">
        <h1 className="text-2xl font-semibold text-graphite-900">{t('admin.categories.title')}</h1>
        <p className="text-sm text-graphite-600">{t('admin.categories.subtitle')}</p>
      </header>

      {categories.isPending ? <p className="text-sm text-graphite-500">{t('app.loading')}</p> : null}
      {categories.isError ? <Alert>{t('admin.categories.error')}</Alert> : null}

      {categories.data ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('admin.categories.listTitle')}</CardTitle>
          </CardHeader>
          <CardContent className="overflow-x-auto p-0">
            {categories.data.length === 0 ? (
              <p className="px-5 py-4 text-sm text-graphite-500">{t('admin.categories.empty')}</p>
            ) : (
              <table className="w-full text-left text-sm">
                <thead className="bg-graphite-50 text-xs text-graphite-500">
                  <tr>
                    <th className="px-4 py-3 font-medium">{t('admin.categories.name')}</th>
                    <th className="px-4 py-3 font-medium">{t('admin.categories.slug')}</th>
                    <th className="px-4 py-3 font-medium">{t('admin.categories.sortOrder')}</th>
                    <th className="px-4 py-3 font-medium">{t('admin.categories.visibility')}</th>
                  </tr>
                </thead>
                <tbody>
                  {categories.data.map((category) => (
                    <tr key={category.id} className="border-t border-graphite-100">
                      <td className="px-4 py-3 font-medium text-graphite-900">{category.name}</td>
                      <td className="px-4 py-3 text-graphite-500">{category.slug}</td>
                      <td className="px-4 py-3 text-graphite-500">{category.sortOrder}</td>
                      <td className="px-4 py-3">
                        <Badge tone={category.isPublished ? 'accent' : 'muted'}>
                          {category.isPublished
                            ? t('productStatuses.Published')
                            : t('productStatuses.Draft')}
                        </Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </CardContent>
        </Card>
      ) : null}

      <Card className="max-w-2xl">
        <CardHeader>
          <CardTitle>{t('admin.categories.createTitle')}</CardTitle>
        </CardHeader>
        <CardContent>
          <form className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
            <div className="flex flex-col gap-1.5">
              <label className={fieldClass} htmlFor="category-name">
                {t('admin.categories.name')}
              </label>
              <Input
                id="category-name"
                aria-invalid={errors.name ? true : undefined}
                {...register('name')}
              />
              {fieldError(errors.name?.message)}
            </div>

            <div className="grid gap-4 sm:grid-cols-2">
              <div className="flex flex-col gap-1.5">
                <label className={fieldClass} htmlFor="category-slug">
                  {t('admin.categories.slug')}
                </label>
                <Input
                  id="category-slug"
                  aria-invalid={errors.slug ? true : undefined}
                  {...register('slug')}
                />
                <p className="text-xs text-graphite-500">{t('admin.categories.slugHint')}</p>
                {fieldError(errors.slug?.message)}
              </div>

              <div className="flex flex-col gap-1.5">
                <label className={fieldClass} htmlFor="category-sort-order">
                  {t('admin.categories.sortOrder')}
                </label>
                <Input
                  id="category-sort-order"
                  inputMode="numeric"
                  aria-invalid={errors.sortOrder ? true : undefined}
                  {...register('sortOrder')}
                />
                {fieldError(errors.sortOrder?.message)}
              </div>
            </div>

            <div className="flex flex-col gap-1.5">
              <label className={fieldClass} htmlFor="category-description">
                {t('admin.categories.description')}
              </label>
              <Textarea id="category-description" rows={3} {...register('description')} />
            </div>

            {createCategory.isError ? (
              <Alert>{describeApiProblem(toApiProblem(createCategory.error))}</Alert>
            ) : null}

            {createCategory.isSuccess ? (
              <p role="status" className="text-sm text-graphite-600">
                {t('admin.categories.created')}
              </p>
            ) : null}

            <Button type="submit" className="self-start" disabled={createCategory.isPending}>
              {createCategory.isPending
                ? t('admin.categories.saving')
                : t('admin.categories.create')}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

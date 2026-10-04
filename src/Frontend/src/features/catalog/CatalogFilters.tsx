import type { FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import type { Category } from '@/lib/api/types';

/** Значения фильтров каталога: хранятся в query-строке, поэтому ссылку можно переслать. */
export interface CatalogFilterValues {
  search: string;
  categoryId: string;
  solventType: string;
}

interface CatalogFiltersProps {
  categories: Category[];
  values: CatalogFilterValues;
  onChange: (values: CatalogFilterValues) => void;
}

export function CatalogFilters({ categories, values, onChange }: CatalogFiltersProps) {
  const { t } = useTranslation();

  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const data = new FormData(event.currentTarget);

    onChange({
      search: String(data.get('search') ?? '').trim(),
      solventType: String(data.get('solventType') ?? '').trim(),
      categoryId: values.categoryId,
    });
  };

  const labelClass = 'text-xs font-medium text-graphite-500';
  const selectClass =
    'h-10 w-full rounded-lg border border-graphite-300 bg-white px-3 text-sm text-graphite-900 ' +
    'focus:border-accent-500 focus:ring-2 focus:ring-accent-300/60 focus:outline-none';

  return (
    <form
      onSubmit={submit}
      className="grid gap-4 rounded-xl border border-graphite-200 bg-white p-4 md:grid-cols-[1.4fr_1fr_1fr_auto]"
    >
      <div className="flex flex-col gap-1.5">
        <label className={labelClass} htmlFor="catalog-search">
          {t('catalog.searchLabel')}
        </label>
        <Input
          id="catalog-search"
          name="search"
          key={`search-${values.search}`}
          defaultValue={values.search}
          placeholder={t('catalog.searchPlaceholder')}
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <label className={labelClass} htmlFor="catalog-category">
          {t('catalog.categoryLabel')}
        </label>
        <select
          id="catalog-category"
          className={selectClass}
          value={values.categoryId}
          onChange={(event) => onChange({ ...values, categoryId: event.target.value })}
        >
          <option value="">{t('catalog.allCategories')}</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1.5">
        <label className={labelClass} htmlFor="catalog-solvent">
          {t('catalog.solventLabel')}
        </label>
        <Input
          id="catalog-solvent"
          name="solventType"
          key={`solvent-${values.solventType}`}
          defaultValue={values.solventType}
          placeholder={t('catalog.solventPlaceholder')}
        />
      </div>

      <div className="flex items-end gap-2">
        <Button type="submit">{t('catalog.searchAction')}</Button>
        <Button
          variant="ghost"
          onClick={() => onChange({ search: '', categoryId: '', solventType: '' })}
        >
          {t('catalog.reset')}
        </Button>
      </div>
    </form>
  );
}

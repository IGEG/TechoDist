import { useTranslation } from 'react-i18next';

/** Заглушка на время подгрузки ленивого чанка (админ-панель). */
export function RouteFallback() {
  const { t } = useTranslation();

  return <p className="py-16 text-center text-sm text-graphite-500">{t('app.loading')}</p>;
}

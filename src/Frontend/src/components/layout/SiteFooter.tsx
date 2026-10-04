import { useTranslation } from 'react-i18next';

export function SiteFooter() {
  const { t } = useTranslation();
  const year = new Date().getFullYear();

  return (
    <footer className="border-t border-graphite-200 bg-white">
      <div className="mx-auto flex w-full max-w-6xl flex-col gap-1 px-4 py-6 text-sm text-graphite-500">
        <span>
          © {year} {t('footer.rights')}
        </span>
        <span>{t('footer.note')}</span>
      </div>
    </footer>
  );
}

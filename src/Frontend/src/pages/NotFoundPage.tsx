import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { buttonVariants } from '@/components/ui/button-variants';
import { Card, CardContent } from '@/components/ui/card';
import { cn } from '@/lib/utils';

export function NotFoundPage() {
  const { t } = useTranslation();

  return (
    <Card className="mx-auto max-w-lg">
      <CardContent className="flex flex-col items-center gap-3 py-10 text-center">
        <span className="text-4xl font-bold text-graphite-300">404</span>
        <h1 className="text-xl font-semibold text-graphite-900">{t('notFound.title')}</h1>
        <p className="text-sm text-graphite-600">{t('notFound.subtitle')}</p>
        <Link to="/catalog" className={cn(buttonVariants({ variant: 'primary' }), 'mt-2')}>
          {t('notFound.action')}
        </Link>
      </CardContent>
    </Card>
  );
}

import { LogOut } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router';
import { Button } from '@/components/ui/button';
import { buttonVariants } from '@/components/ui/button-variants';
import { Badge, Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuth } from '@/features/auth/use-auth';
import { cn } from '@/lib/utils';

/**
 * Разделы админ-панели. Товары и категории открыты (фаза 8); заказы и пользователи —
 * следующие срезы админки, ссылки появятся вместе с их страницами.
 */
const sections = [
  { key: 'products', to: '/admin/products' },
  { key: 'categories', to: '/admin/categories' },
  { key: 'orders', to: null },
  { key: 'users', to: null },
] as const;

export function AdminDashboardPage() {
  const { t } = useTranslation();
  const { session, logout } = useAuth();

  // RequireAuth уже гарантирует сессию; проверка нужна только для типизации.
  if (!session) {
    return null;
  }

  return (
    <div className="flex flex-col gap-6">
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h1 className="text-2xl font-semibold text-graphite-900">{t('admin.dashboardTitle')}</h1>
          <p className="text-sm text-graphite-600">{t('admin.dashboardSubtitle')}</p>
        </div>
        <Button variant="outline" onClick={logout}>
          <LogOut className="h-4 w-4" />
          {t('nav.logout')}
        </Button>
      </header>

      <Card>
        <CardHeader className="flex flex-col gap-1">
          <CardTitle>{t('admin.hello', { name: session.displayName })}</CardTitle>
          {session.email ? <p className="text-sm text-graphite-500">{session.email}</p> : null}
        </CardHeader>
        <CardContent className="flex flex-wrap items-center gap-2">
          <span className="text-sm text-graphite-500">{t('admin.rolesLabel')}</span>
          {session.roles.map((role) => (
            <Badge key={role} tone="accent">
              {role}
            </Badge>
          ))}
        </CardContent>
      </Card>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {sections.map((section) => (
          <Card key={section.key}>
            <CardContent className="flex flex-col gap-2">
              <span className="text-sm font-medium text-graphite-800">
                {t(`admin.sections.${section.key}`)}
              </span>
              {section.to ? (
                <Link
                  to={section.to}
                  className={cn(buttonVariants({ variant: 'outline', size: 'sm' }), 'self-start')}
                >
                  {t('admin.manage')}
                </Link>
              ) : (
                <Badge tone="muted">{t('admin.soon')}</Badge>
              )}
            </CardContent>
          </Card>
        ))}
      </div>

      <Card>
        <CardContent className="flex flex-col gap-3">
          <p className="text-sm text-graphite-600">{t('admin.planned')}</p>
          <Link to="/catalog" className={cn(buttonVariants({ variant: 'primary' }), 'self-start')}>
            {t('admin.goToCatalog')}
          </Link>
        </CardContent>
      </Card>
    </div>
  );
}

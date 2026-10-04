import { useState } from 'react';
import { Menu, X } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Link, NavLink } from 'react-router';
import { Button } from '@/components/ui/button';
import { useAuth } from '@/features/auth/use-auth';
import { cn } from '@/lib/utils';

const navLinkClass = ({ isActive }: { isActive: boolean }): string =>
  cn(
    'rounded-lg px-3 py-2 text-sm font-medium transition-colors',
    isActive ? 'bg-graphite-100 text-graphite-900' : 'text-graphite-600 hover:bg-graphite-50',
  );

export function SiteHeader() {
  const { t } = useTranslation();
  const { session, logout } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);

  return (
    <header className="sticky top-0 z-10 border-b border-graphite-200 bg-white/95 backdrop-blur">
      <div className="mx-auto flex w-full max-w-6xl items-center justify-between gap-4 px-4 py-3">
        <Link to="/" className="flex items-center gap-2.5" onClick={() => setMenuOpen(false)}>
          <span className="grid h-9 w-9 place-items-center rounded-lg bg-graphite-800 text-sm font-bold text-accent-400">
            TD
          </span>
          <span className="flex flex-col leading-tight">
            <span className="text-base font-semibold text-graphite-900">{t('app.name')}</span>
            <span className="hidden text-xs text-graphite-500 sm:block">{t('app.tagline')}</span>
          </span>
        </Link>

        <nav className="hidden items-center gap-1 md:flex">
          <NavLink to="/catalog" className={navLinkClass}>
            {t('nav.catalog')}
          </NavLink>
          <NavLink to="/admin" className={navLinkClass}>
            {t('nav.admin')}
          </NavLink>
        </nav>

        <div className="hidden items-center gap-3 md:flex">
          {session ? (
            <>
              <span className="text-sm text-graphite-600">{session.displayName}</span>
              <Button variant="outline" size="sm" onClick={logout}>
                {t('nav.logout')}
              </Button>
            </>
          ) : (
            <Link to="/admin/login">
              <Button size="sm">{t('nav.login')}</Button>
            </Link>
          )}
        </div>

        <Button
          variant="ghost"
          size="sm"
          className="md:hidden"
          aria-expanded={menuOpen}
          aria-label={t('nav.catalog')}
          onClick={() => setMenuOpen((open) => !open)}
        >
          {menuOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
        </Button>
      </div>

      {menuOpen && (
        <nav className="flex flex-col gap-1 border-t border-graphite-100 px-4 py-3 md:hidden">
          <NavLink to="/catalog" className={navLinkClass} onClick={() => setMenuOpen(false)}>
            {t('nav.catalog')}
          </NavLink>
          <NavLink to="/admin" className={navLinkClass} onClick={() => setMenuOpen(false)}>
            {t('nav.admin')}
          </NavLink>
          {session ? (
            <Button variant="outline" size="sm" className="mt-1" onClick={logout}>
              {t('nav.logout')}
            </Button>
          ) : (
            <NavLink to="/admin/login" className={navLinkClass} onClick={() => setMenuOpen(false)}>
              {t('nav.login')}
            </NavLink>
          )}
        </nav>
      )}
    </header>
  );
}

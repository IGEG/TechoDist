import { lazy, Suspense } from 'react';
import { Navigate, Route, Routes } from 'react-router';
import { AppLayout } from '@/components/layout/AppLayout';
import { RequireAuth } from '@/features/auth/RequireAuth';
import { AdminRoles } from '@/features/auth/roles';
import { CatalogPage } from '@/pages/CatalogPage';
import { NotFoundPage } from '@/pages/NotFoundPage';
import { ProductPage } from '@/pages/ProductPage';
import { RouteFallback } from '@/pages/RouteFallback';

// Каталог грузится сразу (публичная витрина), админ-панель — отдельным чанком.
const AdminLoginPage = lazy(() =>
  import('@/pages/admin/AdminLoginPage').then((module) => ({ default: module.AdminLoginPage })),
);
const AdminDashboardPage = lazy(() =>
  import('@/pages/admin/AdminDashboardPage').then((module) => ({
    default: module.AdminDashboardPage,
  })),
);

/**
 * Маршруты витрины. Все страницы живут внутри AppLayout (шапка/подвал),
 * админ-разделы дополнительно закрыты RequireAuth.
 */
export function App() {
  return (
    <Suspense fallback={<RouteFallback />}>
      <Routes>
        <Route element={<AppLayout />}>
          <Route index element={<Navigate to="/catalog" replace />} />
          <Route path="catalog" element={<CatalogPage />} />
          <Route path="catalog/:productId" element={<ProductPage />} />
          <Route path="admin/login" element={<AdminLoginPage />} />

          {/* Достаточно роли витрины: Manager редактирует каталог, Admin — всё. */}
          <Route element={<RequireAuth roles={[AdminRoles.Admin, AdminRoles.Manager]} />}>
            <Route path="admin" element={<AdminDashboardPage />} />
          </Route>

          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </Suspense>
  );
}

import { lazy, Suspense } from 'react';
import { Navigate, Route, Routes } from 'react-router';
import { AppLayout } from '@/components/layout/AppLayout';
import { RequireAuth } from '@/features/auth/RequireAuth';
import { AdminRoles } from '@/features/auth/roles';
import { BasketPage } from '@/pages/BasketPage';
import { CatalogPage } from '@/pages/CatalogPage';
import { CheckoutPage } from '@/pages/CheckoutPage';
import { NotFoundPage } from '@/pages/NotFoundPage';
import { OrderStatusPage } from '@/pages/OrderStatusPage';
import { ProductPage } from '@/pages/ProductPage';
import { RouteFallback } from '@/pages/RouteFallback';

// Витрина (каталог, корзина, оформление заявки) грузится сразу, админ-панель — отдельными чанками.
const AdminLoginPage = lazy(() =>
  import('@/pages/admin/AdminLoginPage').then((module) => ({ default: module.AdminLoginPage })),
);
const AdminDashboardPage = lazy(() =>
  import('@/pages/admin/AdminDashboardPage').then((module) => ({
    default: module.AdminDashboardPage,
  })),
);
const AdminProductFormPage = lazy(() =>
  import('@/pages/admin/AdminProductFormPage').then((module) => ({
    default: module.AdminProductFormPage,
  })),
);
const AdminProductsPage = lazy(() =>
  import('@/pages/admin/AdminProductsPage').then((module) => ({
    default: module.AdminProductsPage,
  })),
);
const AdminCategoriesPage = lazy(() =>
  import('@/pages/admin/AdminCategoriesPage').then((module) => ({
    default: module.AdminCategoriesPage,
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
          <Route path="cart" element={<BasketPage />} />
          <Route path="checkout" element={<CheckoutPage />} />
          <Route path="orders" element={<OrderStatusPage />} />
          <Route path="orders/:number" element={<OrderStatusPage />} />
          <Route path="admin/login" element={<AdminLoginPage />} />

          {/* Достаточно роли витрины: Manager редактирует каталог, Admin — всё. */}
          <Route element={<RequireAuth roles={[AdminRoles.Admin, AdminRoles.Manager]} />}>
            <Route path="admin" element={<AdminDashboardPage />} />
            <Route path="admin/products" element={<AdminProductsPage />} />
            <Route path="admin/products/new" element={<AdminProductFormPage />} />
            <Route path="admin/products/:productId/edit" element={<AdminProductFormPage />} />
            <Route path="admin/categories" element={<AdminCategoriesPage />} />
          </Route>

          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Routes>
    </Suspense>
  );
}

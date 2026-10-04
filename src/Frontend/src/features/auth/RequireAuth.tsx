import type { ReactNode } from 'react';
import { Navigate, Outlet, useLocation } from 'react-router';
import { selectSession, useAuthStore } from './auth-store';

interface RequireAuthProps {
  /** Роли, любой из которых достаточно (например ['Admin'] — только супер-админ). */
  roles?: string[];
  children?: ReactNode;
}

/**
 * Guard маршрутов админ-панели: без сессии отправляет на вход,
 * с недостаточной ролью — обратно в каталог (проверка на клиенте только для UX,
 * настоящую проверку делает API по ролям из токена).
 */
export function RequireAuth({ roles, children }: RequireAuthProps) {
  const session = useAuthStore(selectSession);
  const location = useLocation();

  if (!session) {
    return <Navigate to="/admin/login" state={{ from: location.pathname }} replace />;
  }

  const allowed = !roles?.length || roles.some((role) => session.roles.includes(role));

  return allowed ? (children ?? <Outlet />) : <Navigate to="/" replace />;
}

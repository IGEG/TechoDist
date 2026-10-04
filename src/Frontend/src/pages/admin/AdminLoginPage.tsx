import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Navigate, useLocation, useNavigate } from 'react-router';
import { z } from 'zod';
import { Alert } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { toLoginErrorMessage } from '@/features/auth/auth-api';
import { useAuth } from '@/features/auth/use-auth';

const loginSchema = z.object({
  email: z.string().min(1, 'Укажите e-mail').email('Некорректный e-mail'),
  password: z.string().min(1, 'Укажите пароль'),
});

type LoginFormValues = z.infer<typeof loginSchema>;

/** Вход администратора: парольный grant OpenIddict через шлюз. */
export function AdminLoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { session, login } = useAuth();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });

  // Куда вернуть после входа: RequireAuth кладёт исходный путь в state.
  const from = (location.state as { from?: string } | null)?.from ?? '/admin';

  if (session) {
    return <Navigate to={from} replace />;
  }

  const onSubmit = handleSubmit(async (values) => {
    try {
      await login.mutateAsync(values);
      navigate(from, { replace: true });
    } catch {
      // Текст ошибки берём из login.error — TanStack Query хранит результат mutation.
    }
  });

  const fieldError = (message: string | undefined) =>
    message ? <p className="text-xs text-red-600">{message}</p> : null;

  return (
    <Card className="mx-auto mt-4 max-w-md">
      <CardHeader className="flex flex-col gap-1">
        <CardTitle>{t('admin.loginTitle')}</CardTitle>
        <p className="text-sm text-graphite-500">{t('admin.loginSubtitle')}</p>
      </CardHeader>

      <CardContent>
        <form className="flex flex-col gap-4" onSubmit={onSubmit} noValidate>
          <div className="flex flex-col gap-1.5">
            <label className="text-sm font-medium text-graphite-600" htmlFor="login-email">
              {t('admin.email')}
            </label>
            <Input
              id="login-email"
              type="email"
              autoComplete="username"
              aria-invalid={errors.email ? true : undefined}
              {...register('email')}
            />
            {fieldError(errors.email?.message)}
          </div>

          <div className="flex flex-col gap-1.5">
            <label className="text-sm font-medium text-graphite-600" htmlFor="login-password">
              {t('admin.password')}
            </label>
            <Input
              id="login-password"
              type="password"
              autoComplete="current-password"
              aria-invalid={errors.password ? true : undefined}
              {...register('password')}
            />
            {fieldError(errors.password?.message)}
          </div>

          {login.isError ? <Alert>{toLoginErrorMessage(login.error)}</Alert> : null}

          <Button type="submit" size="lg" disabled={login.isPending}>
            {login.isPending ? t('admin.submitting') : t('admin.submit')}
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

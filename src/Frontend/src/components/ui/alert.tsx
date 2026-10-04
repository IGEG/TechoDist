import type { ReactNode } from 'react';
import { cn } from '@/lib/utils';

interface AlertProps {
  tone?: 'error' | 'info';
  children: ReactNode;
  className?: string;
}

const alertTones: Record<NonNullable<AlertProps['tone']>, string> = {
  error: 'border-red-200 bg-red-50 text-red-700',
  info: 'border-graphite-200 bg-graphite-50 text-graphite-700',
};

/** Сообщение с `role="alert"` — используется для ошибок загрузки и валидации форм. */
export function Alert({ tone = 'error', className, children }: AlertProps) {
  return (
    <p role="alert" className={cn('rounded-lg border px-4 py-3 text-sm', alertTones[tone], className)}>
      {children}
    </p>
  );
}

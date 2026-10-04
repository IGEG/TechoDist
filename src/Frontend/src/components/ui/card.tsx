import type { ComponentProps, ReactNode } from 'react';
import { cn } from '@/lib/utils';

export function Card({ className, ...props }: ComponentProps<'section'>) {
  return (
    <section
      className={cn('rounded-xl border border-graphite-200 bg-white shadow-sm', className)}
      {...props}
    />
  );
}

export function CardHeader({ className, ...props }: ComponentProps<'header'>) {
  return <header className={cn('border-b border-graphite-100 px-5 py-4', className)} {...props} />;
}

export function CardTitle({ className, children, ...props }: ComponentProps<'h2'>) {
  return (
    <h2 className={cn('text-lg font-semibold text-graphite-900', className)} {...props}>
      {children}
    </h2>
  );
}

export function CardContent({ className, ...props }: ComponentProps<'div'>) {
  return <div className={cn('px-5 py-4', className)} {...props} />;
}

interface BadgeProps extends ComponentProps<'span'> {
  tone?: 'neutral' | 'accent' | 'muted';
  children: ReactNode;
}

const badgeTones: Record<NonNullable<BadgeProps['tone']>, string> = {
  neutral: 'bg-graphite-100 text-graphite-700',
  accent: 'bg-accent-300/60 text-graphite-900',
  muted: 'bg-graphite-50 text-graphite-500',
};

export function Badge({ tone = 'neutral', className, children, ...props }: BadgeProps) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full px-2.5 py-1 text-xs font-medium',
        badgeTones[tone],
        className,
      )}
      {...props}
    >
      {children}
    </span>
  );
}

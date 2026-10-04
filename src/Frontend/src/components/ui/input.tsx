import type { ComponentProps } from 'react';
import { cn } from '@/lib/utils';

export function Input({ className, ...props }: ComponentProps<'input'>) {
  return (
    <input
      className={cn(
        'h-10 w-full rounded-lg border border-graphite-300 bg-white px-3 text-sm text-graphite-900',
        'placeholder:text-graphite-400 focus:border-accent-500 focus:ring-2 focus:ring-accent-300/60 focus:outline-none',
        'disabled:cursor-not-allowed disabled:opacity-60',
        className,
      )}
      {...props}
    />
  );
}

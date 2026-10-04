import { cva, type VariantProps } from 'class-variance-authority';

/** Варианты кнопки вынесены в .ts: их переиспользуют ссылки `<Link>` (react-refresh не любит смешения). */
export const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 rounded-lg font-medium transition-colors ' +
    'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent-500 ' +
    'disabled:pointer-events-none disabled:opacity-60',
  {
    variants: {
      variant: {
        primary: 'bg-graphite-800 text-white hover:bg-graphite-700',
        accent: 'bg-accent-500 text-graphite-900 hover:bg-accent-400',
        outline: 'border border-graphite-300 bg-white text-graphite-800 hover:border-graphite-400',
        ghost: 'text-graphite-700 hover:bg-graphite-100',
      },
      size: {
        sm: 'h-9 px-3 text-sm',
        md: 'h-10 px-4 text-sm',
        lg: 'h-12 px-6 text-base',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'md',
    },
  },
);

export type ButtonVariantProps = VariantProps<typeof buttonVariants>;

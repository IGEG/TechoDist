import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

/** Склейка классов Tailwind с разрешением конфликтов (соглашение shadcn/ui). */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}

/** Формат цены: «1 250 ₽» — разделитель разрядов и валюта рядом с числом. */
export function formatPrice(price: number, currency: string): string {
  const formatted = new Intl.NumberFormat('ru-RU', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(price);

  return currency === 'RUB' ? `${formatted} ₽` : `${formatted} ${currency}`;
}

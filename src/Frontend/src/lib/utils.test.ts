import { describe, expect, it } from 'vitest';
import { cn, formatPrice } from './utils';

describe('cn', () => {
  it('разрешает конфликт классов Tailwind в пользу последнего', () => {
    expect(cn('p-2', 'p-4')).toBe('p-4');
  });

  it('отбрасывает ложные значения', () => {
    const maybeClasses: (string | false | undefined | null)[] = [
      'text-graphite-500',
      false,
      undefined,
      null,
      'underline',
    ];

    expect(cn(...maybeClasses)).toBe('text-graphite-500 underline');
  });

  it('добавляет класс по условию', () => {
    const isActive: boolean = Boolean('manager');

    expect(cn('px-3', isActive && 'font-medium')).toBe('px-3 font-medium');
  });
});

describe('formatPrice', () => {
  it('форматирует рубли с разделителем разрядов', () => {
    // ru-RU использует неразрывный пробел как разделитель разрядов.
    expect(formatPrice(1250, 'RUB')).toBe('1\u00A0250 ₽');
  });

  it('сохраняет копейки без лишних нулей', () => {
    expect(formatPrice(1250.5, 'RUB')).toBe('1\u00A0250,5 ₽');
  });

  it('для других валют выводит код вместо символа', () => {
    expect(formatPrice(1250, 'USD')).toBe('1\u00A0250 USD');
  });
});

import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, type RenderOptions } from '@testing-library/react';
import type { ReactElement, ReactNode } from 'react';
import { MemoryRouter } from 'react-router';

interface RenderWithProvidersOptions extends Omit<RenderOptions, 'wrapper'> {
  /** Стартовый маршрут для MemoryRouter (по умолчанию «/»). */
  route?: string;
}

/**
 * Рендер компонента в тех же провайдерах, что и приложение (Query + Router),
 * с изолированным QueryClient: кэш не протекает между тестами.
 */
export function renderWithProviders(ui: ReactElement, options: RenderWithProvidersOptions = {}) {
  const { route = '/', ...renderOptions } = options;

  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, staleTime: 0 } },
  });

  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[route]}>{children}</MemoryRouter>
    </QueryClientProvider>
  );

  return {
    queryClient,
    ...render(ui, { wrapper, ...renderOptions }),
  };
}

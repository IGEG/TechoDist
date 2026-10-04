import { QueryClient } from '@tanstack/react-query';

/** Каталог меняется нечасто: 30 секунд считаем данные свежими, окно не перезапрашиваем. */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});

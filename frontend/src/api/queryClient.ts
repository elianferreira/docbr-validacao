import { QueryClient } from '@tanstack/react-query'

/**
 * Uma instância por app, criada FORA do componente: dentro do `_layout` cada render
 * recriaria o cache.
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, staleTime: 30_000 },
  },
})

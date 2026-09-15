import { useCallback, useMemo } from 'react'
import { router, useLocalSearchParams } from 'expo-router'
import type { z } from 'zod'

/**
 * Estado de filtro vive na URL, nunca em `useState` (§09.6): a tela tem de ser
 * compartilhável por link. Faz o papel do `validateSearch` do TanStack Router.
 *
 * Duas armadilhas tratadas aqui, as duas medidas em app real:
 *
 * - **String vazia não é valor.** `?bloco=` chega como `''`; num `z.coerce.number()`
 *   viraria `0` — filtro que ninguém pediu. Chave vazia sai antes do parse.
 * - **Remover filtro é `undefined`, não `''`.** `setParams({ x: undefined })` tira o
 *   param da URL; `''` o deixa lá vazio e a próxima leitura recria o problema acima.
 */
export function useFiltrosDaUrl<S extends z.ZodType<object>>(
  schema: S,
): [z.infer<S>, (patch: Partial<Record<keyof z.infer<S>, unknown>>) => void] {
  const brutos = useLocalSearchParams()

  const valores = useMemo(() => {
    const limpos: Record<string, unknown> = {}
    for (const [chave, valor] of Object.entries(brutos)) {
      if (valor === '' || valor === undefined) continue
      limpos[chave] = valor
    }
    return schema.parse(limpos) as z.infer<S>
  }, [brutos, schema])

  const aplicar = useCallback((patch: Partial<Record<keyof z.infer<S>, unknown>>) => {
    const normalizado: Record<string, string | undefined> = {}
    for (const [chave, valor] of Object.entries(patch)) {
      normalizado[chave] =
        valor === undefined || valor === null || valor === '' ? undefined : String(valor)
    }
    router.setParams(normalizado)
  }, [])

  return [valores, aplicar]
}

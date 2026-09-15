import type { ReactNode } from 'react'

/**
 * `expo-router` de mentira para os testes de unidade (§09.8).
 *
 * O pacote real traz sintaxe Flow do react-native no bundle
 * (`SyntaxError: Unexpected token 'typeof'`) e nenhum teste de unidade quer o
 * roteador de verdade. O alias está no `vitest.config.mts`.
 *
 * `Link` sai como `<a href>` para `getByRole('link', { name })` funcionar.
 */
let caminhoAtual = '/'
const navegacoes: string[] = []
let params: Record<string, unknown> = {}
const paramsGravados: Record<string, unknown>[] = []

/** Define o pathname que o `usePathname` devolve (item ativo do menu depende dele). */
export function definirCaminho(caminho: string): void {
  caminhoAtual = caminho
}

/** Rotas visitadas via `router.push`/`replace`. */
export function navegacoesFeitas(): string[] {
  return navegacoes
}

/** Semeia os search params da "URL" (o que `useLocalSearchParams` devolve). */
export function definirParams(novos: Record<string, unknown>): void {
  params = { ...novos }
}

/** Patches passados a `router.setParams` — é por aqui que o teste confere "o filtro foi para a URL". */
export function paramsDefinidos(): Record<string, unknown>[] {
  return paramsGravados
}

export function limparRota(): void {
  caminhoAtual = '/'
  navegacoes.length = 0
  params = {}
  paramsGravados.length = 0
}

export function usePathname(): string {
  return caminhoAtual
}

export const router = {
  push: (destino: string) => {
    navegacoes.push(destino)
    caminhoAtual = destino
  },
  replace: (destino: string) => {
    navegacoes.push(destino)
    caminhoAtual = destino
  },
  back: () => {},
  setParams: (patch: Record<string, unknown>) => {
    paramsGravados.push(patch)
    params = { ...params, ...patch }
  },
}

export function useRouter() {
  return router
}

export function useLocalSearchParams<T = Record<string, string>>(): T {
  return params as T
}

export function useGlobalSearchParams<T = Record<string, string>>(): T {
  return {} as T
}

export function Link({
  href,
  children,
  onPress,
  ...resto
}: {
  href: string
  children?: ReactNode
  onPress?: () => void
  style?: unknown
  [chave: string]: unknown
}) {
  // `style` do RN (objeto/array) não serve para a âncora do DOM.
  delete resto.style
  delete resto.dataSet
  return (
    <a
      href={href}
      onClick={(e) => {
        e.preventDefault()
        onPress?.()
        router.push(href)
      }}
      {...(resto as Record<string, unknown>)}
    >
      {children}
    </a>
  )
}

export function Redirect({ href }: { href: string }) {
  router.replace(href)
  return null
}

export function Slot({ children }: { children?: ReactNode }) {
  return <>{children}</>
}

export function Stack({ children }: { children?: ReactNode }) {
  return <>{children}</>
}

Stack.Screen = function Screen() {
  return null
}

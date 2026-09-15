import { QueryClientProvider } from '@tanstack/react-query'
import { SafeAreaProvider } from 'react-native-safe-area-context'
import { RootShell } from '@/componentes/layout/RootShell'
import { TemaProvider } from '@/tema/TemaContext'
import { queryClient } from '@/api/queryClient'
// Sem extensao: o Metro escolhe `.web.ts` (titulo e CSS de :hover) ou `.ts` (nativo,
// vazio). Com a extensao explicita, o web carregaria a versao nativa sem erro nenhum.
import '@/tema/documentoWeb'

/**
 * O UNICO layout do app (§09.3 do `FRONTEND_STACK.md`). A ordem dos providers e
 * contrato: tema e cache existem antes de qualquer tela.
 *
 * Nao ha `AuthProvider` nem `Portao`: este servico e publico e nao tem sessao. E a
 * unica divergencia da §09.3/§09.4, registrada no `AGENTS.md` com a justificativa.
 *
 * `<Slot />` (dentro do `RootShell`), nao `<Stack />`: o app e um painel, nao quer
 * transicao nativa de pilha, e o Slot deixa a casca fixa ao redor da rota.
 */
export default function RootLayout() {
  return (
    <SafeAreaProvider>
      <TemaProvider>
        <QueryClientProvider client={queryClient}>
          <RootShell />
        </QueryClientProvider>
      </TemaProvider>
    </SafeAreaProvider>
  )
}

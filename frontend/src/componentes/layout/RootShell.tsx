import { Slot, usePathname } from 'expo-router'
import { AppLayout } from './AppLayout'
import type { ItemNav } from './AppLayout'

/**
 * Casca fixa em volta de qualquer rota (§09.5). Fica FORA do `<Slot />`, entao o menu
 * e o header nao remontam a cada navegacao.
 *
 * Menu e um **array**. Aqui nenhum item depende de permission (o servico e publico),
 * mas a forma e a mesma: item condicional sairia por `filter`, nunca por `if` no JSX.
 */
const NAV: ItemNav[] = [
  { para: '/validar', rotulo: 'Validar' },
  { para: '/identificar', rotulo: 'Identificar' },
  { para: '/cnh', rotulo: 'Ler CNH' },
]

/**
 * Titulo e subtitulo do header por rota. O `staticData` tipado do TanStack Router nao
 * tem equivalente no Expo Router, entao e um mapa — com as rotas **mais especificas
 * primeiro**, porque a busca e pelo prefixo mais longo.
 */
const TITULOS: Array<[string, { titulo: string; subtitulo?: string }]> = [
  [
    '/validar',
    {
      titulo: 'Validar documento',
      subtitulo: 'Confere a estrutura do numero. Nao prova que o documento existe.',
    },
  ],
  [
    '/identificar',
    {
      titulo: 'Identificar documento',
      subtitulo: 'Descobre que tipos aceitam um numero sem rotulo.',
    },
  ],
  [
    '/cnh',
    {
      titulo: 'Ler CNH de arquivo',
      subtitulo: 'OCR sobre imagem ou PDF. A leitura e uma heuristica, nao uma certeza.',
    },
  ],
]

export function RootShell() {
  const caminho = usePathname()

  const meta = TITULOS.find(([prefixo]) => caminho.startsWith(prefixo))?.[1]

  return (
    <AppLayout
      nomeApp="DocBr"
      itensNav={NAV}
      titulo={meta?.titulo ?? 'DocBr'}
      subtitulo={meta?.subtitulo}
    >
      <Slot />
    </AppLayout>
  )
}

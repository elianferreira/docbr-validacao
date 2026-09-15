/**
 * Tokens institucionais em JS — não existe cascade, `@media` nem `className` em RN.
 * **Dois mapas obrigatórios** (claro e escuro): tema escuro é regra do padrão, não
 * opcional (`FRONTEND_STACK.md` §01).
 *
 * Hex oficiais da paleta Univali. Não inventar tom: se faltar um, pedir ao design.
 */

export const paleta = {
  univali: {
    50: '#e8f1fb',
    100: '#c7dcf5',
    400: '#248aff',
    700: '#004787',
    800: '#003a6e',
    900: '#002b52',
  },
  cinza: {
    50: '#f8fafc',
    100: '#f1f5f9',
    200: '#e2e8f0',
    400: '#94a3b8',
    600: '#475569',
    800: '#1e293b',
    900: '#0f172a',
  },
  branco: '#ffffff',
  preto: '#000000',
  vermelho: '#b3261e',
  ambar: '#8a6100',
  esmeralda: '#0f7a52',
} as const

export interface Cores {
  pagina: string
  superficie: string
  borda: string
  texto: string
  textoFraco: string
  marca: string
  marcaTexto: string
  erro: string
  aviso: string
  sucesso: string
}

const claro: Cores = {
  pagina: paleta.cinza[50],
  superficie: paleta.branco,
  borda: paleta.cinza[200],
  texto: paleta.cinza[900],
  textoFraco: paleta.cinza[600],
  marca: paleta.univali[700],
  marcaTexto: paleta.branco,
  erro: paleta.vermelho,
  aviso: paleta.ambar,
  sucesso: paleta.esmeralda,
}

const escuro: Cores = {
  pagina: paleta.cinza[900],
  superficie: paleta.cinza[800],
  borda: paleta.cinza[600],
  texto: paleta.cinza[50],
  textoFraco: paleta.cinza[400],
  marca: paleta.univali[400],
  marcaTexto: paleta.cinza[900],
  erro: '#f2b8b5',
  aviso: '#ffd48a',
  sucesso: '#6ee7b7',
}

export const mapas = { claro, escuro } as const
export type NomeTema = keyof typeof mapas

export const raios = { sm: 4, md: 8, lg: 12, cheio: 999 } as const
export const espacos = { xs: 4, sm: 8, md: 16, lg: 24, xl: 32 } as const

import type { NomeTema } from './tokens'

/**
 * Preferência de tema no WEB. `localStorage` porque a escolha deve sobreviver ao
 * fechar a aba (diferente do token, que vive na sessão).
 *
 * O `try/catch` não é zelo: Safari com "bloquear todos os cookies" lança
 * `SecurityError` só de **tocar** em `localStorage`, e sem ele o app cai em tela
 * branca no primeiro render.
 */
const CHAVE = 'tema'

function loja(): Storage | null {
  try {
    return typeof localStorage !== 'undefined' ? localStorage : null
  } catch {
    return null
  }
}

export async function lerPreferencia(): Promise<NomeTema | null> {
  const valor = loja()?.getItem(CHAVE)
  return valor === 'claro' || valor === 'escuro' ? valor : null
}

export async function gravarPreferencia(tema: NomeTema | null): Promise<void> {
  try {
    if (tema === null) loja()?.removeItem(CHAVE)
    else loja()?.setItem(CHAVE, tema)
  } catch {
    // Idem: conveniência não derruba a UI.
  }
}

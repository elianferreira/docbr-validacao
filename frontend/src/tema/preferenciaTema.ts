import * as SecureStore from 'expo-secure-store'
import type { NomeTema } from './tokens'

/**
 * Preferência de tema no NATIVO. Não é credencial, mas o SecureStore é o único
 * storage que este template já traz (o plano proíbe `AsyncStorage` para token e não
 * vale adicionar dependência por uma chave). Par web em `preferenciaTema.web.ts`.
 */
const CHAVE = 'tema'

export async function lerPreferencia(): Promise<NomeTema | null> {
  try {
    const valor = await SecureStore.getItemAsync(CHAVE)
    return valor === 'claro' || valor === 'escuro' ? valor : null
  } catch {
    return null
  }
}

export async function gravarPreferencia(tema: NomeTema | null): Promise<void> {
  try {
    if (tema === null) await SecureStore.deleteItemAsync(CHAVE)
    else await SecureStore.setItemAsync(CHAVE, tema)
  } catch {
    // Preferência é conveniência: falha de escrita não pode derrubar a UI.
  }
}

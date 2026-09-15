import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { useColorScheme } from 'react-native'
import { lerPreferencia, gravarPreferencia } from './preferenciaTema'
import { mapas, type Cores, type NomeTema } from './tokens'

/**
 * Tema claro + escuro (§01 do `FRONTEND_STACK.md`): **os dois são obrigatórios**.
 *
 * Regra: segue o SO (`app.json` → `userInterfaceStyle: "automatic"`, que é o que faz
 * o `useColorScheme` mudar) **e** aceita troca manual, persistida por plataforma
 * (`preferenciaTema`, com par `.web`). `null` = "seguir o sistema"; qualquer outro
 * valor é escolha explícita do usuário e vence o SO.
 */
interface Contexto {
  tema: NomeTema
  cores: Cores
  /** `null` quando está seguindo o sistema. */
  escolha: NomeTema | null
  alternar: () => void
  seguirSistema: () => void
}

const TemaCtx = createContext<Contexto | null>(null)

export function TemaProvider({ children }: { children: React.ReactNode }) {
  const doSistema: NomeTema = useColorScheme() === 'dark' ? 'escuro' : 'claro'
  const [escolha, setEscolha] = useState<NomeTema | null>(null)

  // Preferência gravada só é lida no boot; enquanto isso vale o SO.
  useEffect(() => {
    let vivo = true
    lerPreferencia().then((valor) => {
      if (vivo && valor) setEscolha(valor)
    })
    return () => {
      vivo = false
    }
  }, [])

  const tema = escolha ?? doSistema

  const alternar = useCallback(() => {
    setEscolha((atual) => {
      const proximo: NomeTema = (atual ?? doSistema) === 'claro' ? 'escuro' : 'claro'
      void gravarPreferencia(proximo)
      return proximo
    })
  }, [doSistema])

  const seguirSistema = useCallback(() => {
    setEscolha(null)
    void gravarPreferencia(null)
  }, [])

  const valor = useMemo<Contexto>(
    () => ({ tema, cores: mapas[tema], escolha, alternar, seguirSistema }),
    [tema, escolha, alternar, seguirSistema],
  )

  return <TemaCtx.Provider value={valor}>{children}</TemaCtx.Provider>
}

export function useTema(): Contexto {
  const ctx = useContext(TemaCtx)
  if (!ctx) throw new Error('useTema fora do TemaProvider')
  return ctx
}

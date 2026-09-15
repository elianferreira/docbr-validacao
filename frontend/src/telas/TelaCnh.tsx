import { Text } from 'react-native'
import { Cartao } from '@/componentes/Cartao'
import { useTema } from '@/tema/TemaContext'

/**
 * Variante NATIVA da leitura de CNH.
 *
 * O upload real vive em `TelaCnh.web.tsx`: ele depende de `<input type="file">` e
 * `File`, que so existem no DOM. No nativo o equivalente e `expo-document-picker` +
 * `expo-image-picker`, que este app ainda nao declara — quando declarar, esta e a
 * tela a implementar.
 *
 * O par `.tsx`/`.web.tsx` e o jeito do padrao de divergir por plataforma. Quem importa
 * usa `@/telas/TelaCnh` **sem extensao**: com a extensao explicita o Metro resolve o
 * literal e o web carregaria justamente esta versao, sem erro nenhum.
 */
export function TelaCnh() {
  const { cores } = useTema()

  return (
    <Cartao titulo="Disponivel no navegador">
      <Text style={{ color: cores.textoFraco }}>
        A leitura de CNH por arquivo ainda roda so na versao web deste app.
      </Text>
    </Cartao>
  )
}

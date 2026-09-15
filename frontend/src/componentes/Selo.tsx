import { StyleSheet, Text, View } from 'react-native'
import { useTema } from '@/tema/TemaContext'
import { espacos, raios } from '@/tema/tokens'

/** Tom do selo. Mapeia para as cores semanticas do tema, nunca para um hex solto. */
export type TomDoSelo = 'sucesso' | 'erro' | 'aviso' | 'neutro'

interface Props {
  tom: TomDoSelo
  texto: string
}

/**
 * Selo de estado, usado pelo resultado da validacao e pela confianca de cada candidato
 * do OCR. Existe como componente porque duas telas mostram o mesmo par cor + rotulo.
 *
 * Em RN nao ha cascade nem `className`: a cor vem de `useTema()` a cada render, e e o
 * que faz claro/escuro funcionar sem duplicar componente.
 */
export function Selo({ tom, texto }: Props) {
  const { cores } = useTema()

  const corDoTom = {
    sucesso: cores.sucesso,
    erro: cores.erro,
    aviso: cores.aviso,
    neutro: cores.textoFraco,
  }[tom]

  return (
    <View style={[estilos.selo, { borderColor: corDoTom }]}>
      <Text style={[estilos.texto, { color: corDoTom }]}>{texto}</Text>
    </View>
  )
}

const estilos = StyleSheet.create({
  selo: {
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: raios.cheio,
    paddingHorizontal: espacos.sm,
    paddingVertical: 2,
    alignSelf: 'flex-start',
  },
  texto: { fontSize: 12, fontWeight: '700' },
})

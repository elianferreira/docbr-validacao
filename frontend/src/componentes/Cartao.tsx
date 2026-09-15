import { StyleSheet, Text, View } from 'react-native'
import { useTema } from '@/tema/TemaContext'
import { espacos, raios } from '@/tema/tokens'

interface Props {
  titulo?: string
  children: React.ReactNode
}

/** Bloco de conteudo da pagina. As tres telas usam o mesmo, por isso e componente. */
export function Cartao({ titulo, children }: Props) {
  const { cores } = useTema()

  return (
    <View style={[estilos.cartao, { backgroundColor: cores.superficie, borderColor: cores.borda }]}>
      {titulo ? <Text style={[estilos.titulo, { color: cores.texto }]}>{titulo}</Text> : null}
      {children}
    </View>
  )
}

const estilos = StyleSheet.create({
  cartao: {
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: raios.lg,
    padding: espacos.lg,
    gap: espacos.sm,
  },
  titulo: { fontSize: 18, fontWeight: '700' },
})

import { StyleSheet, Text, TextInput, View } from 'react-native'
import { useIdentificarDocumento } from '@/api/__generated__'
import { Cartao } from '@/componentes/Cartao'
import { Selo } from '@/componentes/Selo'
import { useFiltrosDaUrl } from '@/rotas/useFiltrosDaUrl'
import { identificarSearchSchema } from '@/rotas/searchSchemas'
import { useTema } from '@/tema/TemaContext'
import { espacos, raios } from '@/tema/tokens'

/**
 * "Que documento e esse?" — valida o numero em todos os tipos e lista os que aceitaram.
 *
 * E lista, nao resposta unica, e isso e do dominio: varios documentos brasileiros tem
 * 11 digitos (CPF, CNH, PIS, RENAVAM) e um mesmo numero passa em mais de um por
 * coincidencia. Apresentar um "o documento e X" aqui seria mentira.
 */
export function TelaIdentificar() {
  const { cores } = useTema()
  const [filtros, aplicar] = useFiltrosDaUrl(identificarSearchSchema)

  const numero = filtros.numero ?? ''

  const identificacao = useIdentificarDocumento(
    { numero },
    { query: { enabled: numero.trim().length > 0 } },
  )

  const tipos = identificacao.data?.items ?? []

  return (
    <View style={estilos.pagina}>
      <Cartao titulo="Numero sem rotulo">
        <TextInput
          value={numero}
          onChangeText={(texto) => aplicar({ numero: texto })}
          placeholder="Cole o numero aqui"
          placeholderTextColor={cores.textoFraco}
          accessibilityLabel="Numero a identificar"
          autoCapitalize="characters"
          autoCorrect={false}
          style={[estilos.campo, { borderColor: cores.borda, color: cores.texto }]}
        />
        <Text style={[estilos.ajuda, { color: cores.textoFraco }]}>
          Mais de um tipo pode aceitar o mesmo numero — por isso a resposta e uma lista.
        </Text>
      </Cartao>

      {numero.trim().length === 0 ? null : identificacao.isPending ? (
        <Cartao>
          <Text style={{ color: cores.textoFraco }}>Conferindo…</Text>
        </Cartao>
      ) : identificacao.isError ? (
        <Cartao>
          <Selo tom="erro" texto="Falhou" />
          <Text style={{ color: cores.textoFraco }}>
            Nao foi possivel identificar agora. Tente novamente.
          </Text>
        </Cartao>
      ) : tipos.length === 0 ? (
        <Cartao titulo="Nenhum tipo aceitou">
          <Selo tom="neutro" texto="Sem correspondencia" />
          <Text style={{ color: cores.textoFraco }}>
            O numero nao passou na validacao de nenhum dos tipos suportados.
          </Text>
        </Cartao>
      ) : (
        <Cartao titulo={`${tipos.length} tipo(s) aceitaram`}>
          {tipos.map((tipo) => (
            <View key={tipo.slug} style={estilos.linha}>
              <Text style={[estilos.nome, { color: cores.texto }]}>{tipo.nome}</Text>
              <Text style={{ color: cores.textoFraco }}>{tipo.formatado}</Text>
              {tipo.experimental ? <Selo tom="aviso" texto="Experimental" /> : null}
            </View>
          ))}
        </Cartao>
      )}
    </View>
  )
}

const estilos = StyleSheet.create({
  pagina: { gap: espacos.md },
  campo: {
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: raios.md,
    paddingHorizontal: espacos.sm,
    paddingVertical: espacos.sm,
    fontSize: 16,
  },
  linha: { gap: 2, paddingVertical: espacos.xs },
  nome: { fontSize: 16, fontWeight: '700' },
  ajuda: { fontSize: 13 },
})

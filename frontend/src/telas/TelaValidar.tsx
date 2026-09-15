import { Pressable, StyleSheet, Text, TextInput, View } from 'react-native'
import { useListarDocumentos, useValidarDocumento } from '@/api/__generated__'
import { Cartao } from '@/componentes/Cartao'
import { Selo } from '@/componentes/Selo'
import { useFiltrosDaUrl } from '@/rotas/useFiltrosDaUrl'
import { validarSearchSchema } from '@/rotas/searchSchemas'
import { useTema } from '@/tema/TemaContext'
import { espacos, raios } from '@/tema/tokens'

/**
 * Valida um numero contra um tipo de documento.
 *
 * Tipo e numero vivem na **URL** (§09.6), nao em `useState`: o resultado e
 * compartilhavel por link, que e o que permite colar um caso no chamado. A validacao
 * em si e um `useQuery` habilitado so quando ha numero — o resultado e funcao pura da
 * URL, entao query e a forma certa, nao mutation.
 */
export function TelaValidar() {
  const { cores } = useTema()
  const [filtros, aplicar] = useFiltrosDaUrl(validarSearchSchema)

  const catalogo = useListarDocumentos()
  const tipos = catalogo.data?.items ?? []
  const tipoAtual = tipos.find((tipo) => tipo.slug === filtros.tipo)

  const numero = filtros.numero ?? ''

  const validacao = useValidarDocumento(
    filtros.tipo,
    { numero },
    { query: { enabled: numero.trim().length > 0 } },
  )

  const resultado = validacao.data

  return (
    <View style={estilos.pagina}>
      <Cartao titulo="Tipo de documento">
        <View style={estilos.tipos}>
          {tipos.map((tipo) => {
            const ativo = tipo.slug === filtros.tipo

            return (
              <Pressable
                key={tipo.slug}
                onPress={() => aplicar({ tipo: tipo.slug })}
                accessibilityRole="button"
                accessibilityLabel={tipo.nome}
                style={[
                  estilos.chip,
                  { borderColor: cores.borda },
                  ativo && { backgroundColor: cores.marca, borderColor: cores.marca },
                ]}
              >
                <Text style={{ color: ativo ? cores.marcaTexto : cores.texto, fontWeight: '600' }}>
                  {tipo.nome}
                </Text>
              </Pressable>
            )
          })}
        </View>

        {tipoAtual ? (
          <Text style={[estilos.ajuda, { color: cores.textoFraco }]}>{tipoAtual.descricao}</Text>
        ) : null}

        {tipoAtual?.experimental ? (
          <Selo tom="aviso" texto="Experimental — sem referencia independente" />
        ) : null}
      </Cartao>

      <Cartao titulo="Numero">
        <TextInput
          value={numero}
          onChangeText={(texto) => aplicar({ numero: texto })}
          placeholder={tipoAtual?.exemplo ?? 'Digite o numero'}
          placeholderTextColor={cores.textoFraco}
          accessibilityLabel="Numero do documento"
          autoCapitalize="characters"
          autoCorrect={false}
          style={[estilos.campo, { borderColor: cores.borda, color: cores.texto }]}
        />

        <View style={estilos.acoes}>
          <Pressable
            onPress={() => aplicar({ numero: tipoAtual?.exemplo })}
            accessibilityRole="button"
            style={[estilos.botao, { borderColor: cores.borda }]}
          >
            <Text style={{ color: cores.texto }}>Preencher com um exemplo</Text>
          </Pressable>

          <Pressable
            onPress={() => aplicar({ numero: undefined })}
            accessibilityRole="button"
            style={[estilos.botao, { borderColor: cores.borda }]}
          >
            <Text style={{ color: cores.texto }}>Limpar</Text>
          </Pressable>
        </View>
      </Cartao>

      {numero.trim().length === 0 ? null : validacao.isPending ? (
        <Cartao>
          <Text style={{ color: cores.textoFraco }}>Validando…</Text>
        </Cartao>
      ) : validacao.isError ? (
        <Cartao>
          <Selo tom="erro" texto="Falhou" />
          <Text style={{ color: cores.textoFraco }}>
            Nao foi possivel validar agora. Tente novamente.
          </Text>
        </Cartao>
      ) : resultado ? (
        <Cartao titulo="Resultado">
          <Selo
            tom={resultado.valido ? 'sucesso' : 'erro'}
            texto={resultado.valido ? 'Valido' : 'Invalido'}
          />

          <Text style={[estilos.numero, { color: cores.texto }]}>{resultado.formatado}</Text>
          <Text style={{ color: cores.textoFraco }}>{resultado.descricao}</Text>

          {resultado.subtipo ? (
            <Text style={{ color: cores.textoFraco }}>Subtipo: {resultado.subtipo}</Text>
          ) : null}

          <Text style={[estilos.ajuda, { color: cores.textoFraco }]}>
            A validacao e estrutural. Ela nao prova que o documento existe nem que e autentico.
          </Text>
        </Cartao>
      ) : null}
    </View>
  )
}

const estilos = StyleSheet.create({
  pagina: { gap: espacos.md },
  tipos: { flexDirection: 'row', flexWrap: 'wrap', gap: espacos.xs },
  chip: {
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: raios.cheio,
    paddingHorizontal: espacos.md,
    paddingVertical: espacos.sm,
  },
  campo: {
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: raios.md,
    paddingHorizontal: espacos.sm,
    paddingVertical: espacos.sm,
    fontSize: 16,
  },
  acoes: { flexDirection: 'row', flexWrap: 'wrap', gap: espacos.xs },
  botao: {
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: raios.md,
    paddingHorizontal: espacos.md,
    paddingVertical: espacos.sm,
  },
  numero: { fontSize: 22, fontWeight: '700' },
  ajuda: { fontSize: 13 },
})

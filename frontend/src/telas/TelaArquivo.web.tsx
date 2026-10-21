import { useEffect, useRef, useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Pressable, StyleSheet, Text, View } from 'react-native'
import { extrairDocumentoDeArquivo, useListarDocumentos } from '@/api/__generated__'
import type { ResultadoExtracaoDto } from '@/api/__generated__/model'
import { Cartao } from '@/componentes/Cartao'
import { Selo } from '@/componentes/Selo'
import type { TomDoSelo } from '@/componentes/Selo'
import { arquivoSearchSchema, TIPOS_COM_OCR } from '@/rotas/searchSchemas'
import { useFiltrosDaUrl } from '@/rotas/useFiltrosDaUrl'
import { useTema } from '@/tema/TemaContext'
import { espacos, raios } from '@/tema/tokens'

/**
 * Le o numero de uma CNH, CPF ou RG a partir de uma imagem ou PDF.
 *
 * O **tipo** vai para a URL (§09.6) e o **arquivo** nao: o tipo e filtro, o arquivo e
 * dado pessoal enviado pelo usuario e nao cabe num link compartilhavel.
 *
 * O tipo tem de ser escolhido, nao adivinhado: CPF e CNH tem os mesmos 11 digitos e uma
 * CNH traz os dois impressos, entao nao ha como o servidor decidir qual dos dois voce
 * queria ler.
 *
 * `useMutation` sobre a funcao gerada pelo Orval, nao `useQuery`: enviar arquivo e
 * efeito, dispara por acao do usuario e nao deve ser refeito por invalidacao de cache.
 */
export function TelaArquivo() {
  const { cores } = useTema()
  const [filtros, aplicar] = useFiltrosDaUrl(arquivoSearchSchema)
  const entrada = useRef<HTMLInputElement | null>(null)
  const [nomeDoArquivo, setNomeDoArquivo] = useState<string | null>(null)

  const catalogo = useListarDocumentos()
  const tipos = (catalogo.data?.items ?? []).filter((tipo) =>
    (TIPOS_COM_OCR as readonly string[]).includes(tipo.slug),
  )
  const tipoAtual = tipos.find((tipo) => tipo.slug === filtros.tipo)

  const extracao = useMutation<ResultadoExtracaoDto, unknown, File>({
    mutationFn: (arquivo) => extrairDocumentoDeArquivo(filtros.tipo, { arquivo }),
  })

  // Trocar de tipo invalida a leitura anterior: ela respondia outra pergunta, e deixar o
  // numero da CNH na tela sob o rotulo "CPF" seria um falso positivo de interface.
  const { reset } = extracao
  useEffect(() => {
    reset()
    setNomeDoArquivo(null)
    if (entrada.current) entrada.current.value = ''
  }, [filtros.tipo, reset])

  const resultado = extracao.data

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

        {tipoAtual?.experimental ? (
          <Text style={[estilos.ajuda, { color: cores.textoFraco }]}>
            Experimental: o {tipoAtual.nome} nao tem padrao nacional nem implementacao de
            referencia, e com menos digitos a validacao acerta por acaso com mais frequencia.
            Trate a leitura como indicio.
          </Text>
        ) : null}
      </Cartao>

      <Cartao titulo={`Arquivo ${tipoAtual ? `do ${tipoAtual.nome}` : ''}`.trim()}>
        {/* `<input type="file">` cru: e a unica forma de abrir o seletor do navegador,
            e por isso esta tela tem variante `.web.tsx`. */}
        <input
          ref={entrada}
          type="file"
          accept=".pdf,.jpg,.jpeg,.png,.webp,.bmp,.tif,.tiff"
          aria-label="Arquivo do documento"
          onChange={(evento) => {
            const arquivo = evento.target.files?.[0]
            if (!arquivo) return
            setNomeDoArquivo(arquivo.name)
            extracao.mutate(arquivo)
          }}
        />

        {nomeDoArquivo ? (
          <Text style={{ color: cores.textoFraco }}>Enviado: {nomeDoArquivo}</Text>
        ) : null}

        {nomeDoArquivo ? (
          <Pressable
            onPress={() => {
              setNomeDoArquivo(null)
              extracao.reset()
              if (entrada.current) entrada.current.value = ''
            }}
            accessibilityRole="button"
            style={[estilos.botao, { borderColor: cores.borda }]}
          >
            <Text style={{ color: cores.texto }}>Limpar</Text>
          </Pressable>
        ) : null}

        <Text style={[estilos.ajuda, { color: cores.textoFraco }]}>
          O arquivo e lido e descartado — nada fica guardado no servidor.
        </Text>
      </Cartao>

      {extracao.isPending ? (
        <Cartao>
          <Text style={{ color: cores.textoFraco }}>Lendo o documento…</Text>
        </Cartao>
      ) : extracao.isError ? (
        <Cartao>
          <Selo tom="erro" texto="Falhou" />
          <Text style={{ color: cores.textoFraco }}>
            Nao foi possivel ler o arquivo. Confira o formato e o tamanho e tente de novo.
          </Text>
        </Cartao>
      ) : resultado ? (
        <>
          <Cartao titulo="Leitura">
            <Selo
              tom={resultado.resposta ? 'sucesso' : 'aviso'}
              texto={resultado.resposta ? 'Numero encontrado' : 'Sem leitura confiavel'}
            />

            {resultado.resposta ? (
              <Text style={[estilos.numero, { color: cores.texto }]}>{resultado.resposta}</Text>
            ) : null}

            <Text style={{ color: cores.textoFraco }}>{resultado.mensagem}</Text>
            <Text style={[estilos.ajuda, { color: cores.textoFraco }]}>
              Digito verificador valido nao e prova de leitura correta: o modulo 11 aceita cerca
              de 1 em 11 sequencias por acaso. So vira resposta o candidato que tambem tem
              evidencia de posicao no documento. Confira com o original.
            </Text>
          </Cartao>

          {resultado.candidatos.length > 0 ? (
            <Cartao titulo={`Candidatos avaliados (${resultado.candidatos.length})`}>
              {resultado.candidatos.map((candidato) => (
                <View key={candidato.numero} style={estilos.linha}>
                  <Text style={[estilos.nome, { color: cores.texto }]}>{candidato.formatado}</Text>
                  <Selo tom={tomDaConfianca(candidato.confianca)} texto={candidato.confianca} />
                  <Text style={{ color: cores.textoFraco }}>{candidato.descricao}</Text>
                  {candidato.contexto ? (
                    <Text style={[estilos.contexto, { color: cores.textoFraco }]}>
                      …{candidato.contexto}…
                    </Text>
                  ) : null}
                </View>
              ))}
            </Cartao>
          ) : null}
        </>
      ) : null}
    </View>
  )
}

/** `Alta`/`Media` sao apresentaveis; `Baixa` e `Invalido` nao viram resposta. */
function tomDaConfianca(confianca: string): TomDoSelo {
  if (confianca === 'Alta') return 'sucesso'
  if (confianca === 'Media') return 'aviso'
  if (confianca === 'Baixa') return 'neutro'
  return 'erro'
}

const estilos = StyleSheet.create({
  pagina: { gap: espacos.md },
  tipos: { flexDirection: 'row', flexWrap: 'wrap', gap: espacos.sm },
  chip: {
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: raios.cheio,
    paddingHorizontal: espacos.md,
    paddingVertical: espacos.xs,
  },
  botao: {
    borderWidth: StyleSheet.hairlineWidth,
    borderRadius: raios.md,
    paddingHorizontal: espacos.md,
    paddingVertical: espacos.sm,
    alignSelf: 'flex-start',
  },
  linha: { gap: 2, paddingVertical: espacos.xs },
  nome: { fontSize: 16, fontWeight: '700' },
  numero: { fontSize: 22, fontWeight: '700' },
  contexto: { fontSize: 12, fontStyle: 'italic' },
  ajuda: { fontSize: 13 },
})

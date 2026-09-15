import { useRef, useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { Pressable, StyleSheet, Text, View } from 'react-native'
import { extrairCnhDeArquivo } from '@/api/__generated__'
import type { ResultadoExtracaoDto } from '@/api/__generated__/model'
import { Cartao } from '@/componentes/Cartao'
import { Selo } from '@/componentes/Selo'
import type { TomDoSelo } from '@/componentes/Selo'
import { useTema } from '@/tema/TemaContext'
import { espacos, raios } from '@/tema/tokens'

/**
 * Le o numero da CNH de uma imagem ou PDF.
 *
 * Aqui o estado NAO vai para a URL, e a excecao a §09.6 e proposital: o conteudo e um
 * arquivo enviado pelo usuario (dado pessoal), que nao cabe nem deve caber num link
 * compartilhavel.
 *
 * `useMutation` sobre a funcao gerada pelo Orval, nao `useQuery`: enviar arquivo e
 * efeito, dispara por acao do usuario e nao deve ser refeito por invalidacao de cache.
 */
export function TelaCnh() {
  const { cores } = useTema()
  const entrada = useRef<HTMLInputElement | null>(null)
  const [nomeDoArquivo, setNomeDoArquivo] = useState<string | null>(null)

  const extracao = useMutation<ResultadoExtracaoDto, unknown, File>({
    mutationFn: (arquivo) => extrairCnhDeArquivo({ arquivo }),
  })

  const resultado = extracao.data

  return (
    <View style={estilos.pagina}>
      <Cartao titulo="Arquivo da CNH">
        {/* `<input type="file">` cru: e a unica forma de abrir o seletor do navegador,
            e por isso esta tela tem variante `.web.tsx`. */}
        <input
          ref={entrada}
          type="file"
          accept=".pdf,.jpg,.jpeg,.png,.webp,.bmp,.tif,.tiff"
          aria-label="Arquivo da CNH"
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

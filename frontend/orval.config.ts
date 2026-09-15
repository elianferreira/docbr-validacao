import { defineConfig } from 'orval'

/**
 * Gera hooks do TanStack Query a partir do OpenAPI do serviço .NET.
 * Rodar: `bun run gen:api`
 * O gerado vai para `src/api/__generated__` (git-ignored, regenerável) e passa pelo
 * mutator (`src/api/mutator.ts`), herdando baseURL, token e o 401 do `client.ts`.
 */
export default defineConfig({
  app: {
    input: {
      // OpenAPI do backend .NET 10 (ajustar para o endereço do seu serviço).
      target: `${process.env.EXPO_API_TARGET ?? 'http://localhost:5000'}/openapi/v1.json`,
    },
    output: {
      mode: 'tags-split',
      target: './src/api/__generated__',
      schemas: './src/api/__generated__/model',
      client: 'react-query',
      httpClient: 'axios',
      override: {
        mutator: {
          path: './src/api/mutator.ts',
          name: 'customInstance',
        },
        query: {
          useQuery: true,
          // `useMutation: true` faz o Orval v8 gerar `useXxx` como MUTATION tambem para
          // os GET — e o hook nunca dispara sozinho, a tela abre vazia e nada vai ao
          // console (medido: zero XHR em /validar). Toda rota deste servico e leitura
          // pura, entao query em tudo; o upload de arquivo usa a funcao gerada dentro de
          // um `useMutation` escrito a mao na tela.
          useMutation: false,
        },
      },
    },
  },
})

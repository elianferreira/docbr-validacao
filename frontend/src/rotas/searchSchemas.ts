import { z } from 'zod'

/**
 * Um schema por rota que tem query (§09.6). O `useLocalSearchParams` do Expo Router
 * entrega **string** (ou `string[]`) sempre — por isso os helpers abaixo. Sem eles o
 * filtro chega como `'2026'` e toda comparacao numerica quebra em silencio.
 *
 * Nome de campo e valor de enum sao **estaveis**: a URL e contrato com o usuario —
 * link salvo tem de continuar valendo depois de refactor.
 */

/** `?ativo=true` / `?ativo=1`. Ausente = `undefined`, nunca `false`. */
export const booleanoDeQuery = z
  .union([z.boolean(), z.enum(['true', 'false', '1', '0'])])
  .transform((v) => v === true || v === 'true' || v === '1')
  .optional()

/** Query param repetido chega como `string[]`; uma ocorrencia so, como `string`. */
export const listaDeQuery = z
  .union([z.string(), z.array(z.string())])
  .transform((v) => (Array.isArray(v) ? v : [v]))
  .optional()

export const numero = z.coerce.number().optional()

/**
 * Filtros da tela de validacao. O numero digitado vai para a URL de proposito: o
 * resultado de uma validacao e compartilhavel por link, e e o que permite colar um
 * caso de suporte no chamado.
 *
 * `tipo` usa `.catch('cpf')`: slug invalido na URL (`?tipo=LIXO`) cai no padrao em
 * vez de derrubar a tela. A lista de slugs e contrato do backend e por isso fica
 * escrita aqui, nao derivada — mudar um slug e mudanca de contrato.
 */
export const validarSearchSchema = z.object({
  tipo: z
    .enum([
      'cpf',
      'cnpj',
      'cnh',
      'certidao',
      'titulo-eleitor',
      'pis',
      'cns',
      'renavam',
      'rg-sp',
    ])
    .catch('cpf'),
  numero: z.string().optional(),
})

export type ValidarSearch = z.infer<typeof validarSearchSchema>

/** Filtros da tela de identificacao: so o numero a investigar. */
export const identificarSearchSchema = z.object({
  numero: z.string().optional(),
})

export type IdentificarSearch = z.infer<typeof identificarSearchSchema>

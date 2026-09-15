import axios from 'axios'

/**
 * Cliente Axios do servico DocBr.
 *
 * `process.env.EXPO_PUBLIC_API_URL` — `import.meta.env.VITE_*` **nao existe no
 * Metro**. E inlinada no bundle em build time, logo **nunca** segredo ali. Na imagem
 * web fica vazia de proposito: o app chama `/api` relativo e o nginx do container
 * proxia; URL absoluta ali fura o proxy e cai no CORS.
 *
 * Nao ha interceptor de token nem de 401: este servico e publico (ver `AGENTS.md`,
 * "Divergencias do padrao"). Ele nao guarda nada — so calcula sobre o numero enviado.
 */
export const apiClient = axios.create({
  baseURL: process.env.EXPO_PUBLIC_API_URL ?? '',
  timeout: 60_000,
})

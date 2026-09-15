import type { AxiosRequestConfig } from 'axios'
import { apiClient } from './client'

/**
 * Mutator dos hooks gerados pelo Orval (ver `orval.config.ts`). Todo request gerado
 * passa por aqui, logo herda baseURL, token e o tratamento de 401 do `client.ts`.
 */
export async function customInstance<T>(config: AxiosRequestConfig): Promise<T> {
  const { data } = await apiClient.request<T>(config)
  return data
}

export default customInstance

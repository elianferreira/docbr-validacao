import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { definirParams, paramsDefinidos } from '@/test/expoRouterFake'
import { TemaProvider } from '@/tema/TemaContext'

// Os hooks gerados pelo Orval batem na rede; o que este teste cobre e a forma da tela
// (filtro vai para a URL, resultado aparece), nao o cliente HTTP.
vi.mock('@/api/__generated__', () => ({
  useListarDocumentos: () => ({
    data: {
      items: [
        {
          slug: 'cpf',
          nome: 'CPF',
          descricao: 'Cadastro de Pessoas Fisicas.',
          tamanho: 11,
          experimental: false,
          exemplo: '354.406.313-31',
        },
        {
          slug: 'rg-sp',
          nome: 'RG (SP)',
          descricao: 'Registro Geral de Sao Paulo.',
          tamanho: 9,
          experimental: true,
          exemplo: '12.345.678-9',
        },
      ],
      total: 2,
      page: 1,
      pageSize: 2,
    },
  }),
  useValidarDocumento: (_slug: string, entrada: { numero: string }) => ({
    isPending: false,
    isError: false,
    data: entrada.numero
      ? {
          tipo: 'CPF',
          experimental: false,
          valido: true,
          motivo: 'Nenhum',
          descricao: 'Documento valido.',
          normalizado: '35440631331',
          formatado: '354.406.313-31',
          subtipo: null,
        }
      : undefined,
  }),
}))

const { TelaValidar } = await import('./TelaValidar')

function montar() {
  return render(
    <TemaProvider>
      <TelaValidar />
    </TemaProvider>,
  )
}

describe('TelaValidar', () => {
  beforeEach(() => {
    definirParams({ tipo: 'cpf' })
  })

  it('lista os tipos vindos do catalogo', () => {
    montar()

    expect(screen.getByLabelText('CPF')).toBeInTheDocument()
    expect(screen.getByLabelText('RG (SP)')).toBeInTheDocument()
  })

  it('manda o tipo escolhido para a URL, nao para useState', async () => {
    montar()

    await userEvent.click(screen.getByLabelText('RG (SP)'))

    expect(paramsDefinidos()).toContainEqual({ tipo: 'rg-sp' })
  })

  it('manda o numero digitado para a URL', async () => {
    montar()

    await userEvent.type(screen.getByLabelText('Numero do documento'), '3')

    expect(paramsDefinidos()).toContainEqual({ numero: '3' })
  })

  it('mostra o resultado quando ha numero na URL', () => {
    definirParams({ tipo: 'cpf', numero: '35440631331' })

    montar()

    expect(screen.getByText('Valido')).toBeInTheDocument()
    expect(screen.getByText('354.406.313-31')).toBeInTheDocument()
  })

  it('avisa que o tipo experimental nao tem referencia independente', () => {
    definirParams({ tipo: 'rg-sp' })

    montar()

    expect(screen.getByText(/Experimental/)).toBeInTheDocument()
  })

  it('cai no tipo padrao quando a URL traz um slug invalido', () => {
    definirParams({ tipo: 'LIXO' })

    // `.catch('cpf')` no schema: valor invalido na URL nao pode derrubar a tela.
    expect(montar).not.toThrow()
  })
})

namespace Api.Host.Common;

/// <summary>
/// Envelope padrao de lista do backend Univali (BACKEND_STACK.md §04) — e o contrato que o
/// DataTable do front consome.
/// </summary>
/// <param name="Items">Os itens da pagina corrente.</param>
/// <param name="Total">Total de itens disponiveis, ignorando a paginacao.</param>
/// <param name="Page">Pagina corrente, iniciando em 1.</param>
/// <param name="PageSize">Quantidade de itens por pagina.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    /// <summary>Envelopa uma colecao que nao e paginada na origem (catalogo fixo, lookup).</summary>
    public static PagedResult<T> Unica(IReadOnlyList<T> itens) =>
        new(itens, itens.Count, 1, itens.Count);
}

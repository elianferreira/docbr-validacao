using Api.Host.Common;
using DocBr.Validacao;

namespace Api.Host.Modules.Documentos.Application;

/// <summary>
/// Caso de uso de validacao de documento. Orquestra o dominio (<see cref="CatalogoDocumentos"/>
/// e os validadores, que sao POCO puro em <c>DocBr.Validacao</c>) e mapeia para DTO na saida.
/// Nao ha persistencia: o dominio e calculo puro, entao o modulo nao tem repositorio.
/// </summary>
public sealed class DocumentosService
{
    /// <summary>Catalogo completo dos tipos suportados, na ordem de exibicao.</summary>
    public PagedResult<DocumentoResumo> Catalogo() =>
        PagedResult<DocumentoResumo>.Unica(
            CatalogoDocumentos.Todos.Select(Resumir).ToList());

    /// <summary>Valida <paramref name="numero"/> contra o tipo <paramref name="slug"/>.</summary>
    /// <returns><c>null</c> quando o slug nao existe no catalogo.</returns>
    public ResultadoValidacaoDto? Validar(string slug, string? numero)
    {
        var validador = CatalogoDocumentos.PorSlug(slug);

        if (validador is null)
        {
            return null;
        }

        var resultado = validador.Validar(numero);

        return new ResultadoValidacaoDto(
            Tipo: validador.Nome,
            Experimental: validador.Experimental,
            Valido: resultado.Valido,
            Motivo: resultado.Motivo.ToString(),
            Descricao: resultado.Descricao,
            Normalizado: resultado.Normalizado,
            Formatado: resultado.Valido ? validador.Formatar(resultado.Normalizado) : resultado.Normalizado,

            // So a certidao sabe dizer se e de nascimento, casamento ou obito.
            Subtipo: validador is ValidadorCertidao certidao ? certidao.TipoDeCertidao(numero) : null);
    }

    /// <summary>Gera um numero valido do tipo pedido, para teste e massa de dados.</summary>
    /// <returns><c>null</c> quando o slug nao existe no catalogo.</returns>
    public ExemploDto? Exemplo(string slug)
    {
        var validador = CatalogoDocumentos.PorSlug(slug);

        return validador is null ? null : new ExemploDto(validador.Gerar(comMascara: true));
    }

    /// <summary>
    /// Devolve todos os tipos que aceitam o numero informado. E lista, nao resposta unica:
    /// varios documentos tem 11 digitos e um mesmo numero passa em mais de um por coincidencia.
    /// </summary>
    public PagedResult<TipoIdentificado> Identificar(string? numero) =>
        PagedResult<TipoIdentificado>.Unica(
            CatalogoDocumentos.Identificar(numero)
                .Select(validador => new TipoIdentificado(
                    validador.Slug,
                    validador.Nome,
                    validador.Experimental,
                    validador.Formatar(numero)))
                .ToList());

    private static DocumentoResumo Resumir(IValidadorDocumento validador) =>
        new(
            validador.Slug,
            validador.Nome,
            validador.Descricao,
            validador.Tamanho,
            validador.Experimental,
            // Um exemplo valido ja serve de placeholder do campo e mostra a mascara esperada.
            validador.Gerar(comMascara: true));
}

namespace DocBr.Validacao;

/// <summary>Reúne todos os validadores disponíveis e resolve qual usar.</summary>
public static class CatalogoDocumentos
{
    private static readonly IValidadorDocumento[] Validadores =
    [
        new ValidadorCpf(),
        new ValidadorCnpj(),
        new ValidadorCnh(),
        new ValidadorCertidao(),
        new ValidadorTituloEleitoral(),
        new ValidadorPis(),
        new ValidadorCns(),
        new ValidadorRenavam(),
        new ValidadorRgSaoPaulo()
    ];

    /// <summary>Todos os validadores, na ordem de exibição.</summary>
    public static IReadOnlyList<IValidadorDocumento> Todos => Validadores;

    /// <summary>Busca um validador pelo <see cref="IValidadorDocumento.Slug"/>.</summary>
    /// <returns>O validador, ou <c>null</c> quando o identificador não existe.</returns>
    public static IValidadorDocumento? PorSlug(string? slug) =>
        Validadores.FirstOrDefault(validador =>
            string.Equals(validador.Slug, slug?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Tenta descobrir de que documento se trata: valida a entrada em todos os validadores
    /// e devolve os que aceitaram.
    /// </summary>
    /// <remarks>
    /// Útil quando o número chega sem rótulo. Vários documentos têm 11 dígitos (CPF, CNH,
    /// PIS, RENAVAM) e um mesmo número pode passar em mais de um por coincidência, então o
    /// resultado é uma lista, não uma resposta única. Validadores marcados como
    /// <see cref="IValidadorDocumento.Experimental"/> ficam por último.
    /// </remarks>
    public static IReadOnlyList<IValidadorDocumento> Identificar(string? documento) =>
        Validadores
            .Where(validador => validador.Validar(documento).Valido)
            .OrderBy(validador => validador.Experimental)
            .ToList();
}

using System.Text.RegularExpressions;
using DocBr.Validacao;

namespace DocBr.Ocr;

/// <summary>
/// O que o extrator precisa saber para procurar um documento específico no texto do OCR:
/// quantos caracteres ele tem, que rótulos aparecem ao lado dele no papel, e quem valida.
/// </summary>
/// <remarks>
/// <para>
/// O rótulo é o que separa leitura de chute. Passar no dígito verificador não basta —
/// o módulo 11 aceita cerca de uma em cada onze sequências por acaso, e o extrator recorta
/// janelas de dentro de sequências maiores, então números válidos que não estão no documento
/// aparecem sozinhos. Só vira resposta o candidato que também tem evidência de posição.
/// </para>
/// <para>
/// Por isso a lista de rótulos é deliberadamente curta e específica do <em>campo</em>, nunca
/// do título do documento: "CNH" e "HABILITAÇÃO" aparecem no cabeçalho e marcariam como
/// rotulado qualquer número da página.
/// </para>
/// </remarks>
public sealed class PerfilDeLeitura
{
    private readonly Regex _sequencia;
    private readonly Regex _rotulos;

    private PerfilDeLeitura(
        IValidadorDocumento validador,
        string[] rotulos,
        bool aceitaX)
    {
        Validador = validador;
        AceitaX = aceitaX;

        var ultimo = aceitaX ? "[0-9X]" : "[0-9]";
        var minimoDoMeio = Math.Max(0, validador.Tamanho - 2);

        // Casa uma corrida de dígitos com os separadores da máscara no meio. O teto de 9
        // caracteres extras deixa passar o caso comum de dois campos colados (um número
        // grudado numa data), que é de onde saem as janelas recortadas.
        _sequencia = new Regex(
            $@"[0-9][0-9\s.\-/]{{{minimoDoMeio},{validador.Tamanho + 9}}}{ultimo}",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Fronteira explícita em vez de `Contains`: "RG" casaria dentro de "ORG. EMISSOR",
        // que está impresso na própria CNH, e todo número da página viraria "rotulado".
        var alternativas = string.Join('|', rotulos.Select(Regex.Escape));

        _rotulos = new Regex(
            $@"(?<![A-Z0-9])(?:{alternativas})(?![A-Z0-9])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    /// <summary>Quem decide se uma sequência é um documento válido.</summary>
    public IValidadorDocumento Validador { get; }

    /// <summary>
    /// Indica que o último caractere pode ser <c>X</c>. Vale para o RG de São Paulo, cujo
    /// dígito verificador é o resto 10 do módulo 11.
    /// </summary>
    public bool AceitaX { get; }

    /// <summary>Identificador estável do tipo lido (o mesmo slug do catálogo).</summary>
    public string Slug => Validador.Slug;

    /// <summary>Nome de exibição do documento.</summary>
    public string Nome => Validador.Nome;

    /// <summary>Quantidade de caracteres do documento, sem máscara.</summary>
    public int Tamanho => Validador.Tamanho;

    /// <summary>Sequências de caracteres no texto do OCR que podem conter o documento.</summary>
    public MatchCollection Sequencias(string texto) => _sequencia.Matches(texto);

    /// <summary>Indica se algum rótulo do campo aparece no trecho informado.</summary>
    public bool TemRotulo(string trecho) => _rotulos.IsMatch(trecho);

    /// <summary>
    /// Reduz a sequência casada aos caracteres que contam. Separadores de máscara saem;
    /// o <c>X</c> só sobrevive quando o perfil o aceita.
    /// </summary>
    public string Normalizar(string bruto)
    {
        var aproveitados = bruto
            .Where(caractere => char.IsDigit(caractere)
                || (AceitaX && (caractere == 'X' || caractere == 'x')))
            .Select(char.ToUpperInvariant);

        return new string(aproveitados.ToArray());
    }

    private static readonly Dictionary<string, PerfilDeLeitura> PorSlug =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // "REGISTRO" é o rótulo do campo na CNH. O título do documento fica de fora
            // de propósito — ver o comentário da classe.
            ["cnh"] = new(
                new ValidadorCnh(),
                ["REGISTRO", "REGISTO", "REG N", "REGN", "N REG", "Nº REG"],
                aceitaX: false),

            ["cpf"] = new(
                new ValidadorCpf(),
                ["CPF", "C.P.F", "CIC"],
                aceitaX: false),

            // O RG paulista tem nove caracteres e o verificador pode ser X. "SSP" entra
            // porque o órgão emissor costuma estar impresso junto do número.
            ["rg-sp"] = new(
                new ValidadorRgSaoPaulo(),
                ["RG", "R.G", "REGISTRO GERAL", "IDENTIDADE", "SSP"],
                aceitaX: true),
        };

    /// <summary>Os tipos que o OCR sabe procurar, na ordem de exibição.</summary>
    public static IReadOnlyList<PerfilDeLeitura> Todos => [.. PorSlug.Values];

    /// <summary>Busca o perfil pelo slug do catálogo.</summary>
    /// <returns>O perfil, ou <c>null</c> quando o OCR não sabe procurar aquele tipo.</returns>
    public static PerfilDeLeitura? PorTipo(string? slug) =>
        slug is not null && PorSlug.TryGetValue(slug.Trim(), out var perfil) ? perfil : null;
}

using DocBr.Validacao;

namespace DocBr.Ocr;

/// <summary>
/// Quanto se pode confiar em um candidato que passou na validação.
/// </summary>
/// <remarks>
/// Passar no módulo 11 não basta: aproximadamente 1 em cada 11 sequências de 11 dígitos
/// passa por acaso. Como o extrator recorta janelas de dentro de sequências maiores, uma
/// data colada a outro número pode produzir um número válido que não existe no documento
/// — foi medido acontecendo. Por isso um candidato válido só é apresentado como resposta
/// quando também tem evidência de posição.
/// </remarks>
public enum ConfiancaCandidato
{
    /// <summary>Não passou na validação.</summary>
    Invalido = 0,

    /// <summary>Válido, porém recortado de uma sequência maior e sem rótulo por perto. Não serve como resposta.</summary>
    Baixa,

    /// <summary>Válido e com uma evidência de posição: ou estava isolado, ou havia o rótulo "registro" por perto.</summary>
    Media,

    /// <summary>Válido, isolado no texto e junto do rótulo "registro".</summary>
    Alta
}

/// <summary>Um número de 11 dígitos encontrado no documento, já submetido ao validador.</summary>
/// <param name="Numero">Os 11 dígitos, sem máscara.</param>
/// <param name="Formatado">O número na máscara da CNH.</param>
/// <param name="Validacao">Resultado da validação estrutural.</param>
/// <param name="Contexto">Trecho do texto reconhecido em volta do número, para conferência visual.</param>
/// <param name="Rotulado">
/// Indica que o número apareceu perto de uma palavra-chave da CNH ("REGISTRO", "Nº REGISTRO"),
/// o que o torna um candidato mais confiável do que um número solto.
/// </param>
/// <param name="Isolado">
/// Indica que a sequência no texto tinha exatamente 11 dígitos. Um número recortado de dentro
/// de uma sequência maior (CPF colado a uma data, por exemplo) é bem menos confiável.
/// </param>
public sealed record CandidatoCnh(
    string Numero,
    string Formatado,
    ResultadoValidacao Validacao,
    string Contexto,
    bool Rotulado,
    bool Isolado)
{
    /// <summary>Quanto se pode confiar neste candidato. Ver <see cref="ConfiancaCandidato"/>.</summary>
    public ConfiancaCandidato Confianca =>
        !Validacao.Valido ? ConfiancaCandidato.Invalido
        : Isolado && Rotulado ? ConfiancaCandidato.Alta
        : Isolado || Rotulado ? ConfiancaCandidato.Media
        : ConfiancaCandidato.Baixa;
}

/// <summary>Resultado completo da leitura de um arquivo enviado.</summary>
/// <param name="Sucesso">Indica que o arquivo pôde ser lido e passado pelo OCR.</param>
/// <param name="Mensagem">Explicação do que aconteceu, inclusive em caso de falha.</param>
/// <param name="Candidatos">Números de 11 dígitos encontrados, os válidos primeiro.</param>
/// <param name="TextoReconhecido">Texto bruto devolvido pelo OCR, útil para diagnosticar leitura ruim.</param>
/// <param name="PaginasLidas">Quantas páginas/imagens foram processadas.</param>
public sealed record ResultadoExtracao(
    bool Sucesso,
    string Mensagem,
    IReadOnlyList<CandidatoCnh> Candidatos,
    string TextoReconhecido,
    int PaginasLidas)
{
    /// <summary>Candidato mais provável, já ordenado por confiança.</summary>
    public CandidatoCnh? Melhor => Candidatos.Count > 0 ? Candidatos[0] : null;

    /// <summary>
    /// O número que pode ser apresentado como a leitura do documento. É <c>null</c> quando
    /// nenhum candidato alcançou confiança suficiente — inclusive quando existe candidato
    /// válido, mas apenas recortado de uma sequência maior.
    /// </summary>
    public CandidatoCnh? Resposta =>
        Melhor is { Confianca: >= ConfiancaCandidato.Media } candidato ? candidato : null;

    /// <summary>Cria um resultado de falha.</summary>
    public static ResultadoExtracao Falha(string mensagem) =>
        new(false, mensagem, [], string.Empty, 0);
}

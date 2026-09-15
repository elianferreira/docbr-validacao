namespace Api.Host.Configuration;

/// <summary>
/// Tetos do rate limit por IP (secao <c>RateLimit</c>). Todo endpoint anonimo deste servico
/// carrega uma policy — BACKEND_STACK.md §03, ASVS 2.4.1: anonimo sem throttle e regressao
/// de seguranca, porque o limiter global particiona por identidade autenticada e aqui nao
/// existe identidade.
/// </summary>
public sealed class RateLimitOptions
{
    /// <summary>Nome da secao no appsettings / variavel de ambiente (<c>RateLimit__*</c>).</summary>
    public const string SectionName = "RateLimit";

    /// <summary>Nome da policy aplicada as rotas de validacao por numero.</summary>
    public const string PolicyDocumentos = "documentos-por-ip";

    /// <summary>Nome da policy aplicada as rotas de OCR (upload de arquivo).</summary>
    public const string PolicyOcr = "ocr-por-ip";

    /// <summary>Requisicoes por minuto, por IP, nas rotas de validacao por numero.</summary>
    public int DocumentosPorMinuto { get; set; } = 60;

    /// <summary>
    /// Requisicoes por minuto, por IP, nas rotas de OCR. Teto bem menor: cada chamada carrega
    /// um arquivo e ocupa o Tesseract, que e serializado por um semaforo.
    /// </summary>
    public int OcrPorMinuto { get; set; } = 10;
}

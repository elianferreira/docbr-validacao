namespace Api.Host.Configuration;

/// <summary>Configuracao do caminho de OCR (secao <c>Ocr</c>).</summary>
public sealed class OcrOptions
{
    /// <summary>Nome da secao no appsettings / variavel de ambiente (<c>Ocr__*</c>).</summary>
    public const string SectionName = "Ocr";

    /// <summary>Tamanho maximo aceito no upload, em bytes.</summary>
    public long TamanhoMaximoBytes { get; set; } = 20 * 1024 * 1024;

    /// <summary>
    /// Pasta onde a imagem e gravada exatamente como chega ao Tesseract. Vazio desliga.
    /// So para diagnostico: a imagem e dado pessoal e nao deve ser escrita em producao.
    /// </summary>
    public string? PastaDeDiagnostico { get; set; }
}

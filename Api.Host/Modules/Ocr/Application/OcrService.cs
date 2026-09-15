using DocBr.Ocr;

namespace Api.Host.Modules.Ocr.Application;

/// <summary>
/// Caso de uso de leitura de CNH por OCR. O <see cref="ExtratorCnh"/> carrega o modelo de
/// idioma do Tesseract uma vez e NAO e seguro para uso concorrente, entao ele e singleton e
/// as chamadas sao serializadas pelo semaforo deste servico.
/// </summary>
public sealed class OcrService(ExtratorCnh extrator)
{
    private static readonly SemaphoreSlim Trava = new(1, 1);

    /// <summary>Extensoes de arquivo aceitas no upload.</summary>
    public static IReadOnlyList<string> ExtensoesAceitas => ExtratorCnh.ExtensoesAceitas;

    /// <summary>Indica se o nome do arquivo tem uma extensao que o extrator sabe abrir.</summary>
    public static bool ExtensaoSuportada(string nomeDoArquivo) =>
        ExtratorCnh.ExtensaoSuportada(nomeDoArquivo);

    /// <summary>Le o arquivo e devolve os candidatos encontrados.</summary>
    public async Task<ResultadoExtracaoDto> ExtrairAsync(
        byte[] conteudo,
        string nomeDoArquivo,
        CancellationToken cancelamento)
    {
        await Trava.WaitAsync(cancelamento);

        ResultadoExtracao resultado;

        try
        {
            resultado = extrator.Extrair(conteudo, nomeDoArquivo);
        }
        finally
        {
            Trava.Release();
        }

        return new ResultadoExtracaoDto(
            Sucesso: resultado.Sucesso,
            Mensagem: resultado.Mensagem,
            Arquivo: nomeDoArquivo,
            PaginasLidas: resultado.PaginasLidas,
            Resposta: resultado.Resposta?.Formatado,
            TextoReconhecido: resultado.TextoReconhecido,
            Candidatos: resultado.Candidatos.Select(candidato => new CandidatoDto(
                candidato.Numero,
                candidato.Formatado,
                candidato.Validacao.Valido,
                candidato.Validacao.Motivo.ToString(),
                candidato.Validacao.Descricao,
                candidato.Contexto,
                candidato.Rotulado,
                candidato.Isolado,
                candidato.Confianca.ToString())).ToList());
    }

    /// <summary>
    /// Indica se o motor de OCR esta utilizavel — e o que o <c>/health</c> checa, no lugar do
    /// <c>CanConnect</c> do banco: este servico nao tem Oracle, a dependencia externa dele e o
    /// modelo de idioma do Tesseract.
    /// </summary>
    public bool MotorDisponivel => extrator.MotorCarregado;
}

using DocBr.Ocr;

namespace Api.Host.Modules.Ocr.Application;

/// <summary>
/// Caso de uso de leitura de documento por OCR. O <see cref="ExtratorDocumento"/> carrega o
/// modelo de idioma do Tesseract uma vez e NAO e seguro para uso concorrente, entao ele e
/// singleton e as chamadas sao serializadas pelo semaforo deste servico.
/// </summary>
public sealed class OcrService(ExtratorDocumento extrator)
{
    private static readonly SemaphoreSlim Trava = new(1, 1);

    /// <summary>Extensoes de arquivo aceitas no upload.</summary>
    public static IReadOnlyList<string> ExtensoesAceitas => ExtratorDocumento.ExtensoesAceitas;

    /// <summary>Slugs de documento que o OCR sabe procurar, para a mensagem de recusa.</summary>
    public static IReadOnlyList<string> TiposSuportados =>
        [.. PerfilDeLeitura.Todos.Select(perfil => perfil.Slug)];

    /// <summary>Indica se o nome do arquivo tem uma extensao que o extrator sabe abrir.</summary>
    public static bool ExtensaoSuportada(string nomeDoArquivo) =>
        ExtratorDocumento.ExtensaoSuportada(nomeDoArquivo);

    /// <summary>
    /// Le o arquivo e devolve os candidatos ao documento pedido. O tipo vem da rota porque a
    /// busca e por tamanho e rotulo: CPF e CNH tem os mesmos 11 digitos, e uma CNH traz os
    /// dois impressos — sem saber o que se procura, nao ha como escolher entre eles.
    /// </summary>
    public async Task<ResultadoExtracaoDto> ExtrairAsync(
        byte[] conteudo,
        string nomeDoArquivo,
        PerfilDeLeitura perfil,
        CancellationToken cancelamento)
    {
        await Trava.WaitAsync(cancelamento);

        ResultadoExtracao resultado;

        try
        {
            resultado = extrator.Extrair(conteudo, nomeDoArquivo, perfil);
        }
        finally
        {
            Trava.Release();
        }

        return new ResultadoExtracaoDto(
            Sucesso: resultado.Sucesso,
            Mensagem: resultado.Mensagem,
            Tipo: perfil.Slug,
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

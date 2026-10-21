namespace Api.Host.Modules.Ocr.Application;

/// <summary>Um numero candidato encontrado na imagem, com o que sustenta (ou nao) a leitura.</summary>
/// <param name="Numero">Somente os digitos.</param>
/// <param name="Formatado">Numero com mascara.</param>
/// <param name="Valido">Se passou no digito verificador.</param>
/// <param name="Motivo">Motivo da recusa, ou <c>Nenhum</c>.</param>
/// <param name="Descricao">Mensagem legivel da validacao.</param>
/// <param name="Contexto">Trecho do texto reconhecido ao redor do candidato.</param>
/// <param name="Rotulado">O candidato veio logo apos um rotulo esperado no documento.</param>
/// <param name="Isolado">O candidato estava isolado, nao recortado de uma sequencia maior.</param>
/// <param name="Confianca">
/// Alta, Media ou Baixa. Digito verificador valido NAO e evidencia de leitura correta —
/// o modulo 11 aceita cerca de 1 em 11 sequencias por acaso, entao so vira resposta quem e
/// valido E tem evidencia de posicao.
/// </param>
public sealed record CandidatoDto(
    string Numero,
    string Formatado,
    bool Valido,
    string Motivo,
    string Descricao,
    string Contexto,
    bool Rotulado,
    bool Isolado,
    string Confianca);

/// <summary>Resultado da extracao de um numero de documento a partir de um arquivo enviado.</summary>
/// <param name="Sucesso">Se a extracao produziu uma resposta apresentavel.</param>
/// <param name="Mensagem">Explicacao do desfecho, para exibicao.</param>
/// <param name="Tipo">Slug do documento que foi procurado (<c>cnh</c>, <c>cpf</c>, <c>rg-sp</c>).</param>
/// <param name="Arquivo">Nome do arquivo recebido.</param>
/// <param name="PaginasLidas">Quantidade de paginas submetidas ao OCR.</param>
/// <param name="Resposta">Numero ja formatado, quando ha resposta confiavel.</param>
/// <param name="TextoReconhecido">Texto bruto devolvido pelo OCR.</param>
/// <param name="Candidatos">Todos os candidatos avaliados, inclusive os descartados.</param>
public sealed record ResultadoExtracaoDto(
    bool Sucesso,
    string Mensagem,
    string Tipo,
    string Arquivo,
    int PaginasLidas,
    string? Resposta,
    string TextoReconhecido,
    IReadOnlyList<CandidatoDto> Candidatos);

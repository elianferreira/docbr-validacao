using Api.Host.Common;
using Api.Host.Configuration;
using Api.Host.Modules.Ocr.Application;
using DocBr.Ocr;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Host.Modules.Ocr.Api;

/// <summary>Rotas do modulo Ocr.</summary>
public static class OcrEndpoints
{
    /// <summary>Registra o grupo de leitura de arquivo em <c>/api/documentos</c>.</summary>
    public static IEndpointRouteBuilder MapOcrEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Anonimo, logo sob rate limit por IP — e com teto bem menor que o das rotas de
        // numero, porque cada chamada carrega um arquivo e ocupa o Tesseract (§03).
        var grupo = endpoints
            .MapGroup("/api/documentos")
            .WithTags("Ocr")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitOptions.PolicyOcr);

        // O tipo vem na rota, nao e adivinhado: CPF e CNH tem os mesmos 11 digitos e a CNH
        // traz os dois impressos, entao a resposta so faz sentido para um tipo pedido.
        //
        // `IFormFile` no lugar de ler o `HttpRequest` cru: e o que faz o contrato multipart
        // aparecer no /openapi/v1.json, que e de onde o Orval gera o cliente do front.
        grupo.MapPost("/{slug}/arquivo",
            async Task<Results<Ok<ResultadoExtracaoDto>, BadRequest<ProblemDetails>, NotFound<ProblemDetails>>> (
                string slug,
                IFormFile? arquivo,
                OcrService servico,
                IOptions<OcrOptions> opcoes,
                CancellationToken cancelamento) =>
            {
                var perfil = PerfilDeLeitura.PorTipo(slug);

                if (perfil is null)
                {
                    return TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Tipo sem leitura por OCR",
                        Detail = $"O OCR le apenas: {string.Join(", ", OcrService.TiposSuportados)}.",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                var limite = opcoes.Value.TamanhoMaximoBytes;

                if (arquivo is null || arquivo.Length == 0)
                {
                    return TypedResults.BadRequest(Recusa("Nenhum arquivo foi enviado."));
                }

                if (arquivo.Length > limite)
                {
                    return TypedResults.BadRequest(
                        Recusa($"Arquivo maior que {limite / 1024 / 1024} MB."));
                }

                if (!OcrService.ExtensaoSuportada(arquivo.FileName))
                {
                    return TypedResults.BadRequest(Recusa(
                        $"Formato nao suportado. Aceitos: {string.Join(", ", OcrService.ExtensoesAceitas)}."));
                }

                using var memoria = new MemoryStream();
                await arquivo.CopyToAsync(memoria, cancelamento);

                var resultado = await servico.ExtrairAsync(
                    memoria.ToArray(), arquivo.FileName, perfil, cancelamento);

                return TypedResults.Ok(resultado);
            })
            .WithName("ExtrairDocumentoDeArquivo")
            .WithSummary("Le o numero do documento em uma imagem ou PDF enviado.")
            // A resposta carrega numero de documento e o texto lido do documento — dado pessoal.
            .ComDadoPessoalSemCache()
            .DisableAntiforgery();

        return endpoints;
    }

    private static ProblemDetails Recusa(string detalhe) => new()
    {
        Title = "Arquivo invalido",
        Detail = detalhe,
        Status = StatusCodes.Status400BadRequest
    };
}

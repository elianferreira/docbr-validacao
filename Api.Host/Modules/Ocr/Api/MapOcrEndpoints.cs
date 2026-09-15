using Api.Host.Common;
using Api.Host.Configuration;
using Api.Host.Modules.Ocr.Application;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Host.Modules.Ocr.Api;

/// <summary>Rotas do modulo Ocr.</summary>
public static class OcrEndpoints
{
    /// <summary>Registra o grupo <c>/api/documentos/cnh</c>.</summary>
    public static IEndpointRouteBuilder MapOcrEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // Anonimo, logo sob rate limit por IP — e com teto bem menor que o das rotas de
        // numero, porque cada chamada carrega um arquivo e ocupa o Tesseract (§03).
        var grupo = endpoints
            .MapGroup("/api/documentos/cnh")
            .WithTags("Ocr")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitOptions.PolicyOcr);

        // `IFormFile` no lugar de ler o `HttpRequest` cru: e o que faz o contrato multipart
        // aparecer no /openapi/v1.json, que e de onde o Orval gera o cliente do front.
        grupo.MapPost("/arquivo", async Task<Results<Ok<ResultadoExtracaoDto>, BadRequest<ProblemDetails>>> (
                IFormFile? arquivo,
                OcrService servico,
                IOptions<OcrOptions> opcoes,
                CancellationToken cancelamento) =>
            {
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

                var resultado = await servico.ExtrairAsync(memoria.ToArray(), arquivo.FileName, cancelamento);

                return TypedResults.Ok(resultado);
            })
            .WithName("ExtrairCnhDeArquivo")
            .WithSummary("Le o numero da CNH em uma imagem ou PDF enviado.")
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

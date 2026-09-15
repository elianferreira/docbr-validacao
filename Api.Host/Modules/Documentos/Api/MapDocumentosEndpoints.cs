using Api.Host.Common;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Api.Host.Configuration;
using Api.Host.Modules.Documentos.Application;

namespace Api.Host.Modules.Documentos.Api;

/// <summary>Rotas do modulo Documentos.</summary>
public static class DocumentosEndpoints
{
    /// <summary>Registra o grupo <c>/api/documentos</c>.</summary>
    public static IEndpointRouteBuilder MapDocumentosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        // O servico nao tem sessao: todo endpoint e anonimo e, por isso, obrigatoriamente
        // sob rate limit por IP (BACKEND_STACK.md §03).
        var grupo = endpoints
            .MapGroup("/api/documentos")
            .WithTags("Documentos")
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitOptions.PolicyDocumentos);

        grupo.MapGet("/", (DocumentosService servico) =>
                TypedResults.Ok(servico.Catalogo()))
            .WithName("ListarDocumentos")
            .WithSummary("Lista os tipos de documento suportados.");

        // As rotas abaixo recebem ou devolvem numero de documento — dado pessoal, logo no-store.
        grupo.MapPost("/identificar", (EntradaNumero entrada, DocumentosService servico) =>
                TypedResults.Ok(servico.Identificar(entrada.Numero)))
            .WithName("IdentificarDocumento")
            .WithSummary("Descobre quais tipos aceitam o numero informado.")
            .ComDadoPessoalSemCache();

        grupo.MapPost("/{slug}/numero", Results<Ok<ResultadoValidacaoDto>, NotFound<ProblemDetails>> (
                string slug,
                EntradaNumero entrada,
                DocumentosService servico) =>
            {
                var resultado = servico.Validar(slug, entrada.Numero);

                return resultado is null
                    ? TypedResults.NotFound(TipoDesconhecido(slug))
                    : TypedResults.Ok(resultado);
            })
            .WithName("ValidarDocumento")
            .WithSummary("Valida um numero contra um tipo de documento.")
            .ComDadoPessoalSemCache();

        grupo.MapGet("/{slug}/exemplo", Results<Ok<ExemploDto>, NotFound<ProblemDetails>> (
                string slug,
                DocumentosService servico) =>
            {
                var exemplo = servico.Exemplo(slug);

                return exemplo is null
                    ? TypedResults.NotFound(TipoDesconhecido(slug))
                    : TypedResults.Ok(exemplo);
            })
            .WithName("ExemploDeDocumento")
            .WithSummary("Gera um numero sintaticamente valido do tipo pedido.")
            .ComDadoPessoalSemCache();

        return endpoints;
    }

    // O slug vem da URL e e ecoado aqui de proposito: e entrada do cliente, nao detalhe
    // interno. Nada de tabela, SQL ou versao de runtime no corpo (§03 · fingerprint).
    private static ProblemDetails TipoDesconhecido(string slug) => new()
    {
        Title = "Tipo de documento desconhecido",
        Detail = $"Nao existe tipo de documento com o identificador '{slug}'.",
        Status = StatusCodes.Status404NotFound
    };
}

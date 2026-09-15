using Microsoft.Net.Http.Headers;

namespace Api.Host.Common;

/// <summary>
/// Marca as respostas que carregam dado pessoal com <c>Cache-Control: no-store</c>
/// (BACKEND_STACK.md §03 — ASVS 14.3.2). Numero de documento e dado de pessoa: nao pode
/// sobreviver em cache de navegador, proxy ou historico.
/// </summary>
public static class CacheControlExtensions
{
    /// <summary>Aplica <c>no-store</c> a toda resposta produzida pelo grupo ou rota.</summary>
    public static TBuilder ComDadoPessoalSemCache<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(async (contexto, proximo) =>
        {
            // O header tem que ser escrito antes do corpo comecar a sair.
            contexto.HttpContext.Response.OnStarting(estado =>
            {
                var resposta = (HttpResponse)estado;
                resposta.Headers[HeaderNames.CacheControl] = "no-store";
                resposta.Headers[HeaderNames.Pragma] = "no-cache";
                return Task.CompletedTask;
            }, contexto.HttpContext.Response);

            return await proximo(contexto);
        });

        return builder;
    }
}

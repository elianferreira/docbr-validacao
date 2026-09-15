using Api.Host.Modules.Documentos.Application;

namespace Api.Host.Modules.Documentos;

/// <summary>
/// Auto-registro do modulo Documentos. O <c>Program.cs</c> so chama este metodo e o
/// <c>MapDocumentosEndpoints</c> — nada de DI de modulo inline no host (BACKEND_STACK.md §05).
/// </summary>
public static class DocumentosModule
{
    /// <summary>Registra os servicos do modulo no container.</summary>
    public static IServiceCollection AddDocumentosModule(this IServiceCollection services)
    {
        // Os validadores sao stateless e sem dependencia: singleton serve.
        services.AddSingleton<DocumentosService>();

        return services;
    }
}

using Api.Host.Configuration;
using Api.Host.Modules.Ocr.Application;
using DocBr.Ocr;
using Microsoft.Extensions.Options;

namespace Api.Host.Modules.Ocr;

/// <summary>Auto-registro do modulo Ocr (BACKEND_STACK.md §05).</summary>
public static class OcrModule
{
    /// <summary>Registra os servicos do modulo no container.</summary>
    public static IServiceCollection AddOcrModule(this IServiceCollection services)
    {
        // O Tesseract carrega o modelo de idioma uma vez e nao e seguro para uso concorrente:
        // singleton aqui, serializacao no OcrService.
        services.AddSingleton(provedor => new ExtratorCnh
        {
            PastaDeDiagnostico = provedor.GetRequiredService<IOptions<OcrOptions>>().Value.PastaDeDiagnostico
        });
        services.AddSingleton<OcrService>();

        return services;
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Api.Host.Tests;

/// <summary>
/// Sobe a API em memoria para os testes de integracao. Os tetos de rate limit ficam altos
/// aqui para nao contaminar os demais testes; o teste que mede o throttle usa a sua propria
/// fabrica com teto baixo.
/// </summary>
public class AppDeTeste : WebApplicationFactory<Program>
{
    /// <summary>Tetos usados por esta fabrica, em requisicoes por minuto.</summary>
    protected virtual (int Documentos, int Ocr) Tetos => (10_000, 10_000);

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Ocr:TamanhoMaximoBytes"] = "20971520",
                ["RateLimit:DocumentosPorMinuto"] = Tetos.Documentos.ToString(),
                ["RateLimit:OcrPorMinuto"] = Tetos.Ocr.ToString()
            });
        });
    }
}

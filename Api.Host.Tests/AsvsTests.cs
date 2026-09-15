using System.Net;
using System.Net.Http.Json;

namespace Api.Host.Tests;

/// <summary>
/// Testes das regras fixas de seguranca do BACKEND_STACK.md §03. Cada um falha se a protecao
/// sair — e esse o ponto: sao invariantes, nao cobertura de feature. Rodam isolados com
/// <c>dotnet test --filter asvs</c>.
/// </summary>
public sealed class AsvsTests : IClassFixture<AppDeTeste>
{
    private readonly AppDeTeste _app;

    /// <summary>Recebe a fabrica compartilhada da suite.</summary>
    public AsvsTests(AppDeTeste app) => _app = app;

    [Fact]
    [Trait("asvs", "14.3.2")]
    public async Task Validacao_de_numero_responde_com_no_store()
    {
        var cliente = _app.CreateClient();

        var resposta = await cliente.PostAsJsonAsync(
            "/api/documentos/cpf/numero",
            new { numero = "35440631331" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.True(resposta.Headers.CacheControl?.NoStore, "resposta com numero de documento sem no-store");
    }

    [Fact]
    [Trait("asvs", "14.3.2")]
    public async Task Identificacao_responde_com_no_store()
    {
        var cliente = _app.CreateClient();

        var resposta = await cliente.PostAsJsonAsync(
            "/api/documentos/identificar",
            new { numero = "35440631331" });

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.True(resposta.Headers.CacheControl?.NoStore, "resposta com numero de documento sem no-store");
    }

    [Fact]
    [Trait("asvs", "14.3.2")]
    public async Task Exemplo_responde_com_no_store()
    {
        var cliente = _app.CreateClient();

        var resposta = await cliente.GetAsync("/api/documentos/cpf/exemplo");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.True(resposta.Headers.CacheControl?.NoStore, "resposta com numero de documento sem no-store");
    }

    [Fact]
    [Trait("asvs", "fingerprint")]
    public async Task Nenhuma_resposta_expoe_header_Server()
    {
        var cliente = _app.CreateClient();

        foreach (var rota in new[] { "/", "/health", "/api/documentos" })
        {
            var resposta = await cliente.GetAsync(rota);

            Assert.False(
                resposta.Headers.Contains("Server"),
                $"a rota {rota} devolveu o header Server");
        }
    }

    [Fact]
    [Trait("asvs", "fingerprint")]
    public async Task Raiz_nao_publica_runtime_nem_versao()
    {
        var cliente = _app.CreateClient();

        var corpo = await cliente.GetStringAsync("/");

        foreach (var termo in new[] { ".NET", "Kestrel", "ASP.NET", "net10", "Oracle" })
        {
            Assert.DoesNotContain(termo, corpo, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    [Trait("asvs", "fingerprint")]
    public async Task Erro_nao_vaza_stack_trace_nem_detalhe_interno()
    {
        var cliente = _app.CreateClient();

        var resposta = await cliente.GetAsync("/api/documentos/nao-existe/exemplo");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);

        var corpo = await resposta.Content.ReadAsStringAsync();

        foreach (var termo in new[] { "at Api.Host", "System.", "StackTrace", "Exception" })
        {
            Assert.DoesNotContain(termo, corpo, StringComparison.Ordinal);
        }
    }

    [Fact]
    [Trait("asvs", "2.4.1")]
    public async Task Rota_anonima_de_documentos_tem_rate_limit_por_ip()
    {
        // Teto proposital de 3/min: o servico nao tem sessao, entao o limiter por IP e a
        // unica defesa contra brute-force e automacao (§03).
        await using var app = new AppComTetoBaixo();
        var cliente = app.CreateClient();

        var status = new List<HttpStatusCode>();

        for (var tentativa = 0; tentativa < 6; tentativa++)
        {
            var resposta = await cliente.PostAsJsonAsync(
                "/api/documentos/cpf/numero",
                new { numero = "35440631331" });

            status.Add(resposta.StatusCode);
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, status);
    }

    [Fact]
    [Trait("asvs", "2.4.1")]
    public async Task Rota_anonima_de_ocr_tem_rate_limit_por_ip()
    {
        await using var app = new AppComTetoBaixo();
        var cliente = app.CreateClient();

        var status = new List<HttpStatusCode>();

        for (var tentativa = 0; tentativa < 6; tentativa++)
        {
            // Multipart sem arquivo de proposito: a recusa por 400 ja passou pelo limiter.
            using var formulario = new MultipartFormDataContent
            {
                { new StringContent("sem arquivo"), "observacao" }
            };

            var resposta = await cliente.PostAsync("/api/documentos/cnh/arquivo", formulario);

            status.Add(resposta.StatusCode);
        }

        Assert.Contains(HttpStatusCode.TooManyRequests, status);
    }

    /// <summary>Mesma API, com tetos baixos, para provar que o throttle existe de fato.</summary>
    private sealed class AppComTetoBaixo : AppDeTeste
    {
        protected override (int Documentos, int Ocr) Tetos => (3, 3);
    }
}

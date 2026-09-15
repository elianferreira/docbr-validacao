using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Host.Tests;

/// <summary>Contrato das rotas publicas, ponta a ponta pelo <c>WebApplicationFactory</c>.</summary>
public sealed class RotasPublicasTests : IClassFixture<AppDeTeste>
{
    private readonly AppDeTeste _app;

    /// <summary>Recebe a fabrica compartilhada da suite.</summary>
    public RotasPublicasTests(AppDeTeste app) => _app = app;

    [Fact]
    public async Task Raiz_responde_200()
    {
        var resposta = await _app.CreateClient().GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Health_responde_200_com_o_motor_de_ocr_carregado()
    {
        var resposta = await _app.CreateClient().GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }

    [Fact]
    public async Task Catalogo_vem_no_envelope_paginado_com_os_nove_tipos()
    {
        var corpo = await _app.CreateClient().GetStringAsync("/api/documentos");

        using var json = JsonDocument.Parse(corpo);
        var raiz = json.RootElement;

        // PagedResult<T> e o contrato de lista do padrao (§04).
        Assert.Equal(9, raiz.GetProperty("items").GetArrayLength());
        Assert.Equal(9, raiz.GetProperty("total").GetInt32());
        Assert.Equal(1, raiz.GetProperty("page").GetInt32());
    }

    [Theory]
    [InlineData("cpf")]
    [InlineData("cnpj")]
    [InlineData("cnh")]
    [InlineData("certidao")]
    [InlineData("titulo-eleitor")]
    [InlineData("pis")]
    [InlineData("cns")]
    [InlineData("renavam")]
    [InlineData("rg-sp")]
    public async Task Exemplo_gerado_passa_na_propria_validacao(string slug)
    {
        var cliente = _app.CreateClient();

        var exemplo = await cliente.GetFromJsonAsync<JsonElement>($"/api/documentos/{slug}/exemplo");
        var numero = exemplo.GetProperty("numero").GetString();

        var resposta = await cliente.PostAsJsonAsync($"/api/documentos/{slug}/numero", new { numero });
        var validacao = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(validacao.GetProperty("valido").GetBoolean(), $"{slug}: exemplo gerado nao validou");
    }

    [Fact]
    public async Task Tipo_desconhecido_responde_404_em_ProblemDetails()
    {
        var resposta = await _app.CreateClient().GetAsync("/api/documentos/passaporte/exemplo");

        Assert.Equal(HttpStatusCode.NotFound, resposta.StatusCode);

        var problema = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal("Tipo de documento desconhecido", problema.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Certidao_devolve_o_subtipo()
    {
        var cliente = _app.CreateClient();

        var exemplo = await cliente.GetFromJsonAsync<JsonElement>("/api/documentos/certidao/exemplo");
        var numero = exemplo.GetProperty("numero").GetString();

        var resposta = await cliente.PostAsJsonAsync("/api/documentos/certidao/numero", new { numero });
        var validacao = await resposta.Content.ReadFromJsonAsync<JsonElement>();

        Assert.False(string.IsNullOrWhiteSpace(validacao.GetProperty("subtipo").GetString()));
    }

    [Fact]
    public async Task Upload_multipart_sem_arquivo_responde_400()
    {
        using var formulario = new MultipartFormDataContent
        {
            { new StringContent("sem arquivo"), "observacao" }
        };

        var resposta = await _app.CreateClient()
            .PostAsync("/api/documentos/cnh/arquivo", formulario);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }

    [Fact]
    public async Task Upload_de_extensao_nao_suportada_responde_400()
    {
        using var arquivo = new ByteArrayContent([1, 2, 3, 4]);
        using var formulario = new MultipartFormDataContent
        {
            { arquivo, "arquivo", "documento.docx" }
        };

        var resposta = await _app.CreateClient()
            .PostAsync("/api/documentos/cnh/arquivo", formulario);

        Assert.Equal(HttpStatusCode.BadRequest, resposta.StatusCode);
    }
}

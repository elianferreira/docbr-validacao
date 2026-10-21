using DocBr.Ocr;

namespace Api.Host.Tests;

/// <summary>
/// O perfil e o unico lugar onde o OCR sabe que documento esta procurando. Estes testes
/// cobrem as duas armadilhas que a generalizacao introduziu: rotulo curto casando dentro de
/// outra palavra, e o <c>X</c> do RG paulista sumindo na normalizacao.
/// </summary>
public class PerfilDeLeituraTests
{
    [Theory]
    [InlineData("cnh")]
    [InlineData("cpf")]
    [InlineData("rg-sp")]
    public void Conhece_os_tipos_que_o_ocr_le(string slug) =>
        Assert.NotNull(PerfilDeLeitura.PorTipo(slug));

    [Theory]
    [InlineData("cnpj")]
    [InlineData("certidao")]
    [InlineData("")]
    [InlineData(null)]
    public void Recusa_tipo_sem_leitura_por_ocr(string? slug) =>
        Assert.Null(PerfilDeLeitura.PorTipo(slug));

    [Fact]
    public void Rotulo_curto_nao_casa_dentro_de_outra_palavra()
    {
        var rg = PerfilDeLeitura.PorTipo("rg-sp")!;

        // "ORG. EMISSOR" esta impresso na propria CNH. Com `Contains`, o "RG" casaria e todo
        // numero da pagina viraria "rotulado" — que e justamente a evidencia que separa
        // leitura de chute.
        Assert.False(rg.TemRotulo("ORG. EMISSOR / UF / CATEGORIA"));
        Assert.True(rg.TemRotulo("RG 12.345.678-9"));
        Assert.True(rg.TemRotulo("REGISTRO GERAL 12.345.678-9"));
    }

    [Fact]
    public void So_o_rg_paulista_aceita_X_como_verificador()
    {
        var rg = PerfilDeLeitura.PorTipo("rg-sp")!;
        var cpf = PerfilDeLeitura.PorTipo("cpf")!;

        Assert.Equal("12345678X", rg.Normalizar("12.345.678-X"));
        Assert.Equal("3544063133", cpf.Normalizar("354.406.313-3X"));
    }

    [Fact]
    public void Acha_a_sequencia_do_tamanho_de_cada_tipo()
    {
        var rg = PerfilDeLeitura.PorTipo("rg-sp")!;
        var cpf = PerfilDeLeitura.PorTipo("cpf")!;

        Assert.Single(rg.Sequencias("RG 12.345.678-X SSP"));
        Assert.Single(cpf.Sequencias("CPF 354.406.313-31"));

        // Nove caracteres nao alimentam um CPF: o perfil do CPF nao pode devolver candidato
        // para um RG solto na pagina.
        Assert.Empty(cpf.Sequencias("RG 12.345.678"));
    }
}

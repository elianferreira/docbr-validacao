using DocBr.Validacao;

namespace DocBr.Validacao.Tests;

public class ValidadorCnhTests
{
    private readonly ValidadorCnh _validador = new();

    // Vetores calculados pela implementação de referência (validate-docbr, Python)
    // e conferidos contra esta implementação em C#.
    [Theory]
    [InlineData("35440631331")]
    [InlineData("00000000119")]
    [InlineData("12345678900")]
    [InlineData("01234567807")]
    [InlineData("98765432109")]
    [InlineData("10000000091")]
    [InlineData("24681357982")]
    [InlineData("86420975300")]
    [InlineData("70000000087")]
    public void Aceita_cnh_com_digitos_verificadores_corretos(string cnh)
    {
        var resultado = _validador.Validar(cnh);

        Assert.True(resultado.Valido, resultado.Descricao);
        Assert.Equal(MotivoInvalidez.Nenhum, resultado.Motivo);
        Assert.Equal(cnh, resultado.Normalizado);
    }

    // Quando o primeiro dígito estoura o módulo 11 entra o desconto de 2 no segundo dígito.
    // É exatamente aqui que as implementações copiadas de fóruns divergem: elas cortam o
    // resto em 10 antes de subtrair e não tratam o resultado negativo.
    [Theory]
    [InlineData("92947822209")] // resto do segundo dígito = 0  -> 0 - 2 = -2 -> +11 = 9
    [InlineData("58059171800")] // resto do segundo dígito = 1  -> 1 - 2 = -1 -> +11 = 10 -> 0
    [InlineData("54187955208")] // resto do segundo dígito = 10 -> 10 - 2 = 8
    public void Aceita_cnh_que_depende_do_desconto_do_primeiro_digito(string cnh)
    {
        Assert.True(_validador.Validar(cnh).Valido);
    }

    [Theory]
    [InlineData("354 406 313 31")]
    [InlineData("  35440631331  ")]
    [InlineData("354.406.313-31")]
    public void Aceita_cnh_com_mascara_e_espacos(string cnh)
    {
        var resultado = _validador.Validar(cnh);

        Assert.True(resultado.Valido, resultado.Descricao);
        Assert.Equal("35440631331", resultado.Normalizado);
    }

    [Theory]
    [InlineData("35440631332")]
    [InlineData("35440631330")]
    [InlineData("12345678901")]
    public void Recusa_cnh_com_digito_verificador_errado(string cnh)
    {
        var resultado = _validador.Validar(cnh);

        Assert.False(resultado.Valido);
        Assert.Equal(MotivoInvalidez.DigitoVerificador, resultado.Motivo);
    }

    // 11111111111, 55555555555 e 99999999999 passam no cálculo do módulo 11,
    // por isso a regra de dígitos repetidos precisa existir separadamente.
    [Theory]
    [InlineData("11111111111")]
    [InlineData("55555555555")]
    [InlineData("99999999999")]
    [InlineData("00000000000")]
    public void Recusa_cnh_formada_por_um_unico_digito_repetido(string cnh)
    {
        var resultado = _validador.Validar(cnh);

        Assert.False(resultado.Valido);
        Assert.Equal(MotivoInvalidez.DigitosRepetidos, resultado.Motivo);
    }

    [Theory]
    [InlineData("3544063133")]
    [InlineData("354406313311")]
    [InlineData("1")]
    public void Recusa_cnh_com_quantidade_errada_de_digitos(string cnh)
    {
        var resultado = _validador.Validar(cnh);

        Assert.False(resultado.Valido);
        Assert.Equal(MotivoInvalidez.QuantidadeDeDigitos, resultado.Motivo);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Recusa_entrada_vazia(string? cnh)
    {
        var resultado = _validador.Validar(cnh);

        Assert.False(resultado.Valido);
        Assert.Equal(MotivoInvalidez.EntradaVazia, resultado.Motivo);
    }

    [Theory]
    [InlineData("3544063133A")]
    [InlineData("354406313/31")]
    public void Recusa_entrada_com_caractere_invalido(string cnh)
    {
        var resultado = _validador.Validar(cnh);

        Assert.False(resultado.Valido);
        Assert.Equal(MotivoInvalidez.CaractereInvalido, resultado.Motivo);
    }

    [Fact]
    public void Formatar_aplica_a_mascara_padrao()
    {
        Assert.Equal("354 406 313 31", _validador.Formatar("35440631331"));
        Assert.Equal("354 406 313 31", _validador.Formatar("354 406 313 31"));
    }

    [Fact]
    public void Formatar_recusa_entrada_com_tamanho_errado()
    {
        Assert.Throws<ArgumentException>(() => _validador.Formatar("123"));
    }

    [Fact]
    public void Tudo_que_o_gerador_produz_e_aceito_pela_validacao()
    {
        for (var i = 0; i < 20_000; i++)
        {
            var cnh = _validador.Gerar();
            var resultado = _validador.Validar(cnh);

            Assert.True(resultado.Valido, $"CNH gerada foi recusada: {cnh} ({resultado.Descricao})");
        }
    }

    [Fact]
    public void Gerador_com_mascara_produz_documento_formatado_e_valido()
    {
        var cnh = _validador.Gerar(comMascara: true);

        Assert.Matches(@"^\d{3} \d{3} \d{3} \d{2}$", cnh);
        Assert.True(_validador.Validar(cnh).Valido);
    }
}

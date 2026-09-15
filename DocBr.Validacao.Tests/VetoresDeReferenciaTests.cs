using System.Text.Json;
using DocBr.Validacao;

namespace DocBr.Validacao.Tests;

/// <summary>
/// Confere cada validador contra massa produzida pela implementação de referência
/// (validate-docbr, em Python).
/// </summary>
/// <remarks>
/// O arquivo <c>vetores-referencia.json</c> foi gerado executando a referência, não escrito
/// à mão: vetor produzido pela mesma cabeça que escreveu o código só prova consistência
/// interna. Para regerar, veja <c>tools/gerar-vetores.py</c>.
/// </remarks>
public class VetoresDeReferenciaTests
{
    private static readonly Dictionary<string, Vetores> Massa = Carregar();

    private sealed record Vetores(string[] Validos, string[] Amostra);

    private static Dictionary<string, Vetores> Carregar()
    {
        var caminho = Path.Combine(AppContext.BaseDirectory, "vetores-referencia.json");
        var conteudo = File.ReadAllText(caminho);

        var opcoes = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        return JsonSerializer.Deserialize<Dictionary<string, Vetores>>(conteudo, opcoes)
            ?? throw new InvalidOperationException("Arquivo de vetores vazio ou malformado.");
    }

    public static TheoryData<string> Slugs()
    {
        var dados = new TheoryData<string>();

        foreach (var slug in new[] { "cpf", "cnpj", "cnh", "cns", "pis", "renavam", "titulo-eleitor", "certidao" })
            dados.Add(slug);

        return dados;
    }

    [Theory]
    [MemberData(nameof(Slugs))]
    public void Aceita_toda_a_massa_produzida_pela_referencia(string slug)
    {
        var validador = CatalogoDocumentos.PorSlug(slug)!;
        var vetores = Massa[slug];

        Assert.NotEmpty(vetores.Validos);

        var recusados = vetores.Validos
            .Select(documento => new { documento, resultado = validador.Validar(documento) })
            .Where(item => !item.resultado.Valido)
            .Take(5)
            .ToList();

        Assert.True(
            recusados.Count == 0,
            $"{validador.Nome}: recusou documento que a referência considera válido — " +
            string.Join("; ", recusados.Select(item => $"{item.documento} ({item.resultado.Descricao})")));
    }

    [Fact]
    public void Aceita_cnpj_alfanumerico_da_referencia()
    {
        var validador = new ValidadorCnpj();

        foreach (var documento in Massa["cnpj-alfanumerico"].Validos)
            Assert.True(validador.Validar(documento).Valido, $"CNPJ alfanumérico recusado: {documento}");
    }

    [Theory]
    [MemberData(nameof(Slugs))]
    public void Tudo_que_o_gerador_produz_e_aceito_pela_propria_validacao(string slug)
    {
        var validador = CatalogoDocumentos.PorSlug(slug)!;

        for (var i = 0; i < 3_000; i++)
        {
            var documento = validador.Gerar();
            var resultado = validador.Validar(documento);

            Assert.True(resultado.Valido,
                $"{validador.Nome}: gerou documento que a própria validação recusa — {documento} ({resultado.Descricao})");
        }
    }

    [Theory]
    [MemberData(nameof(Slugs))]
    public void Formatar_e_revalidar_preserva_o_resultado(string slug)
    {
        var validador = CatalogoDocumentos.PorSlug(slug)!;

        foreach (var documento in Massa[slug].Amostra)
        {
            var formatado = validador.Formatar(documento);

            Assert.True(validador.Validar(formatado).Valido,
                $"{validador.Nome}: recusou o próprio formato de exibição — {formatado}");
        }
    }

    [Theory]
    [MemberData(nameof(Slugs))]
    public void Recusa_documento_com_um_digito_alterado(string slug)
    {
        var validador = CatalogoDocumentos.PorSlug(slug)!;

        foreach (var documento in Massa[slug].Amostra)
        {
            // Mexe no último caractere, que é sempre verificador em todos estes documentos.
            var ultimo = documento[^1];
            var trocado = ultimo == '9' ? '0' : (char)(ultimo + 1);
            var adulterado = documento[..^1] + trocado;

            Assert.False(validador.Validar(adulterado).Valido,
                $"{validador.Nome}: aceitou documento com o verificador alterado — {adulterado}");
        }
    }
}

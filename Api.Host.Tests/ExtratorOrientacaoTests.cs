using DocBr.Ocr;
using SkiaSharp;

namespace Api.Host.Tests;

/// <summary>
/// Foto de documento em pe tirada com o celular na horizontal chega deitada. Antes, o extrator
/// so corrigia +-5 graus e devolvia ruido para essas fotos; estes testes garantem que os quatro
/// quartos de volta sao lidos.
/// </summary>
public class ExtratorOrientacaoTests
{
    // CPF de teste, gerado pelo validador; nao pertence a ninguem.
    private const string Cpf = "52998224725";

    [Theory]
    [InlineData(0)]
    [InlineData(90)]
    [InlineData(-90)]
    [InlineData(180)]
    public void Le_o_cpf_em_qualquer_quarto_de_volta(int giro)
    {
        using var extrator = new ExtratorDocumento();

        var resultado = extrator.Extrair(
            DocumentoGirado(giro),
            "documento.png",
            PerfilDeLeitura.PorTipo("cpf")!);

        Assert.Contains(resultado.Candidatos, candidato => candidato.Numero == Cpf && candidato.Validacao.Valido);
    }

    private static byte[] DocumentoGirado(int giro)
    {
        const int Largura = 1600;
        const int Altura = 500;

        using var fonte = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 72);
        using var tinta = new SKPaint { Color = SKColors.Black, IsAntialias = true };

        var deitado = Math.Abs(giro) == 90;
        using var bitmap = new SKBitmap(deitado ? Altura : Largura, deitado ? Largura : Altura);
        using var canvas = new SKCanvas(bitmap);

        canvas.Clear(SKColors.White);
        canvas.Translate(bitmap.Width / 2f, bitmap.Height / 2f);
        canvas.RotateDegrees(giro);
        canvas.Translate(-Largura / 2f, -Altura / 2f);

        canvas.DrawText("REPUBLICA FEDERATIVA DO BRASIL", 80, 140, SKTextAlign.Left, fonte, tinta);
        canvas.DrawText("CPF 529.982.247-25", 80, 300, SKTextAlign.Left, fonte, tinta);
        canvas.DrawText("REGISTRO GERAL 6.622.775", 80, 420, SKTextAlign.Left, fonte, tinta);

        using var dados = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        return dados.ToArray();
    }
}

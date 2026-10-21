using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using DocBr.Validacao;
using SkiaSharp;
using Tesseract;

namespace DocBr.Ocr;

/// <summary>
/// Lê um arquivo de documento (JPG, PNG, WEBP, BMP, TIFF ou PDF), extrai o texto por OCR
/// e devolve os números encontrados do tipo pedido, cada um já validado.
/// </summary>
/// <remarks>
/// <para>
/// OCR de foto de documento é uma heurística, não uma leitura confiável: iluminação,
/// foco, holograma e fundo de segurança derrubam a taxa de acerto. Por isso a classe
/// devolve uma <em>lista de candidatos</em> com o texto reconhecido em volta de cada um,
/// e nunca decide sozinha — quem confirma é a pessoa na tela.
/// </para>
/// <para>
/// O que muda de um tipo para outro (tamanho, rótulos do campo, validador) vem no
/// <see cref="PerfilDeLeitura"/>; o pré-processamento da imagem é o mesmo para todos.
/// </para>
/// <para>
/// A validação continua sendo estrutural (módulo 11). Extrair o número de uma foto não
/// prova que a foto é de um documento verdadeiro.
/// </para>
/// </remarks>
public sealed class ExtratorDocumento : IDisposable
{
    private static readonly string[] ExtensoesDeImagem =
        [".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff"];

    private readonly TesseractEngine _engine;
    private bool _descartado;

    /// <summary>Cria o extrator carregando o modelo de idioma do Tesseract.</summary>
    /// <param name="caminhoTessdata">
    /// Pasta com os arquivos <c>.traineddata</c>. Quando omitido, usa a pasta
    /// <c>tessdata</c> ao lado do binário.
    /// </param>
    /// <param name="idioma">Idiomas do Tesseract, separados por <c>+</c>.</param>
    public ExtratorDocumento(string? caminhoTessdata = null, string idioma = "por+eng")
    {
        caminhoTessdata ??= Path.Combine(AppContext.BaseDirectory, "tessdata");

        if (!Directory.Exists(caminhoTessdata))
        {
            throw new DirectoryNotFoundException(
                $"Pasta tessdata não encontrada em '{caminhoTessdata}'. " +
                "Ela contém os modelos de idioma do Tesseract e deve ser copiada para a saída do build.");
        }

        _engine = new TesseractEngine(caminhoTessdata, idioma, EngineMode.Default);
    }

    /// <summary>
    /// Quando preenchido, grava nesta pasta a imagem exatamente como ela chega ao Tesseract.
    /// É a única forma barata de descobrir que o pré-processamento apagou o texto: o OCR
    /// devolve vazio sem nenhum erro, e a imagem preparada é a evidência.
    /// </summary>
    public string? PastaDeDiagnostico { get; init; }

    /// <summary>
    /// Indica que o motor do Tesseract esta carregado e utilizavel. O construtor carrega o
    /// modelo de idioma (e falha alto se a pasta tessdata nao existe), entao isto responde
    /// sem custo e serve de health check.
    /// </summary>
    public bool MotorCarregado => !_descartado;

    /// <summary>Indica se a extensão do arquivo é suportada.</summary>
    public static bool ExtensaoSuportada(string nomeDoArquivo)
    {
        var extensao = Path.GetExtension(nomeDoArquivo).ToLowerInvariant();
        return extensao == ".pdf" || ExtensoesDeImagem.Contains(extensao);
    }

    /// <summary>
    /// Indica se esta plataforma renderiza PDF. O PDFium por tras do PDFtoImage nao cobre todo
    /// sistema operacional; Windows (dev) e Linux (a imagem OracleLinux 10 do deploy) cobre.
    /// Fora deles o PDF e recusado com mensagem, em vez de estourar em runtime.
    /// </summary>
    [SupportedOSPlatformGuard("windows")]
    [SupportedOSPlatformGuard("linux")]
    [SupportedOSPlatformGuard("macos")]
    public static bool PdfSuportado =>
        OperatingSystem.IsWindows()
        || OperatingSystem.IsLinux()
        || OperatingSystem.IsMacOS();

    /// <summary>Lista, para exibição na tela, as extensões aceitas.</summary>
    public static IReadOnlyList<string> ExtensoesAceitas => [".pdf", .. ExtensoesDeImagem];

    /// <summary>Lê o arquivo e devolve os candidatos ao documento pedido.</summary>
    /// <param name="conteudo">Bytes do arquivo enviado.</param>
    /// <param name="nomeDoArquivo">Nome original, usado apenas para descobrir a extensão.</param>
    /// <param name="perfil">Que documento procurar: tamanho, rótulos do campo e validador.</param>
    /// <param name="maximoDePaginas">Teto de páginas de PDF processadas, para limitar o tempo de resposta.</param>
    public ResultadoExtracao Extrair(
        byte[] conteudo,
        string nomeDoArquivo,
        PerfilDeLeitura perfil,
        int maximoDePaginas = 5)
    {
        ArgumentNullException.ThrowIfNull(perfil);
        ObjectDisposedException.ThrowIf(_descartado, this);

        if (conteudo.Length == 0)
            return ResultadoExtracao.Falha("O arquivo enviado está vazio.");

        if (!ExtensaoSuportada(nomeDoArquivo))
        {
            return ResultadoExtracao.Falha(
                $"Formato não suportado. Aceitos: {string.Join(", ", ExtensoesAceitas)}.");
        }

        var ehPdf = Path.GetExtension(nomeDoArquivo).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

        List<byte[]> paginas;

        try
        {
            if (!ehPdf)
            {
                paginas = [conteudo];
            }
            else if (PdfSuportado)
            {
                paginas = RenderizarPdf(conteudo, maximoDePaginas);
            }
            else
            {
                return ResultadoExtracao.Falha(
                    "Esta plataforma nao renderiza PDF. Envie o documento como imagem (JPG ou PNG).");
            }
        }
        catch (Exception excecao)
        {
            return ResultadoExtracao.Falha($"Não foi possível abrir o arquivo: {excecao.Message}");
        }

        if (paginas.Count == 0)
            return ResultadoExtracao.Falha("O PDF não tem páginas legíveis.");

        var texto = new StringBuilder();
        var candidatos = new List<CandidatoDocumento>();

        // Foto deitada é comum (documento em pé fotografado com o celular na horizontal), mas
        // ler a página inteira nos quatro giros custa minutos. O giro é escolhido numa miniatura,
        // e a página em tamanho cheio só é lida nele.
        List<float> orientacoes;

        try
        {
            orientacoes = paginas.Select(EscolherOrientacao).ToList();
        }
        catch (Exception excecao)
        {
            return ResultadoExtracao.Falha($"Falha no OCR: {excecao.Message}");
        }

        // Foto torta é a degradação que mais derruba o OCR, e corrigi-la custa caro.
        // Então só se paga por ela quando a leitura reta não produziu nada confiável.
        foreach (var inclinacao in Inclinacoes)
        {
            for (var indice = 0; indice < paginas.Count; indice++)
            {
                try
                {
                    var preparada = Preparar(paginas[indice], orientacoes[indice] + inclinacao);

                    GravarDiagnostico(preparada, orientacoes[indice] + inclinacao);

                    // Duas passadas: a normal recupera os rótulos do campo, e a restrita a
                    // dígitos costuma acertar o número onde a normal confunde letra com algarismo.
                    texto.AppendLine(ReconhecerTexto(preparada, apenasDigitos: false));
                    texto.AppendLine(ReconhecerTexto(preparada, apenasDigitos: true));
                }
                catch (Exception excecao)
                {
                    return ResultadoExtracao.Falha($"Falha no OCR: {excecao.Message}");
                }
            }

            candidatos = ExtrairCandidatos(texto.ToString(), perfil);

            if (candidatos.Count > 0 && candidatos[0].Confianca >= ConfiancaCandidato.Media)
                break;
        }

        var reconhecido = texto.ToString();

        var mensagem = candidatos.Count switch
        {
            0 => $"Nenhuma sequência de {perfil.Tamanho} caracteres foi encontrada no documento. " +
                 $"Digite o número do documento ({perfil.Nome}) manualmente.",

            _ when candidatos[0].Confianca >= ConfiancaCandidato.Media =>
                $"Número de {perfil.Nome} localizado e validado. Confira contra o documento antes de aceitar.",

            // Este caso é traiçoeiro: existe número válido, mas ele foi recortado de uma
            // sequência maior (data colada a outro campo, por exemplo) e provavelmente não
            // está escrito no documento. Apresentá-lo como leitura seria um falso positivo.
            _ when candidatos[0].Validacao.Valido =>
                "Foi encontrado um número que passa na validação, mas ele saiu do meio de uma " +
                "sequência maior e pode não estar no documento. Confira e digite o número manualmente.",

            _ => $"Foram encontrados números de {perfil.Tamanho} caracteres, mas nenhum passou na " +
                 "validação. O OCR pode ter lido algum dígito errado — confira e corrija manualmente."
        };

        return new ResultadoExtracao(true, mensagem, candidatos, reconhecido, paginas.Count);
    }

    /// <summary>Giros de quarto de volta candidatos a orientação da página.</summary>
    private static readonly float[] Orientacoes = [0f, -90f, 90f, 180f];

    /// <summary>Largura da miniatura usada só para descobrir a orientação.</summary>
    private const int LarguraDaMiniatura = 1200;

    /// <summary>
    /// Descobre em que giro a página está de pé: lê uma miniatura em cada quarto de volta e fica
    /// com o que produz mais palavras reconhecidas com confiança. Texto de cabeça para baixo ou
    /// de lado vira ruído de poucas letras, então a contagem separa bem os giros.
    /// </summary>
    private float EscolherOrientacao(byte[] pagina)
    {
        _engine.SetVariable("tessedit_char_whitelist", string.Empty);

        var melhor = 0f;
        var melhorPontuacao = -1;

        foreach (var orientacao in Orientacoes)
        {
            using var pix = Pix.LoadFromMemory(Preparar(pagina, orientacao, LarguraDaMiniatura));
            using var leitura = _engine.Process(pix, PageSegMode.SparseText);

            var pontuacao = PalavrasConfiaveis(leitura);

            if (pontuacao > melhorPontuacao)
            {
                melhorPontuacao = pontuacao;
                melhor = orientacao;
            }
        }

        return melhor;
    }

    private static int PalavrasConfiaveis(Page leitura)
    {
        const float ConfiancaMinima = 70f;

        var total = 0;
        using var iterador = leitura.GetIterator();
        iterador.Begin();

        do
        {
            var palavra = iterador.GetText(PageIteratorLevel.Word);

            if (palavra is not null
                && palavra.Count(char.IsLetterOrDigit) >= 3
                && iterador.GetConfidence(PageIteratorLevel.Word) >= ConfiancaMinima)
            {
                total++;
            }
        }
        while (iterador.Next(PageIteratorLevel.Word));

        return total;
    }

    private string ReconhecerTexto(byte[] imagem, bool apenasDigitos)
    {
        _engine.SetVariable("tessedit_char_whitelist", apenasDigitos ? "0123456789 " : string.Empty);

        using var pix = Pix.LoadFromMemory(imagem);
        using var pagina = _engine.Process(pix, PageSegMode.SparseText);

        return pagina.GetText();
    }

    /// <summary>Quantos candidatos recusados são devolvidos, além de todos os válidos.</summary>
    private const int TetoDeCandidatosInvalidos = 6;

    private static List<CandidatoDocumento> ExtrairCandidatos(string texto, PerfilDeLeitura perfil)
    {
        var textoNormalizado = texto.ToUpperInvariant();
        var vistos = new HashSet<string>();
        var candidatos = new List<CandidatoDocumento>();

        foreach (Match ocorrencia in perfil.Sequencias(texto))
        {
            var caracteres = perfil.Normalizar(ocorrencia.Value);
            var isolado = caracteres.Length == perfil.Tamanho;

            // Uma sequência longa (um número colado a uma data, por exemplo) pode conter o
            // documento, então percorremos todas as janelas do tamanho certo dentro dela.
            // Isso fabrica muitos números falsos, por isso a janela recortada nasce marcada
            // como não isolada e cai para o fim da ordenação.
            for (var inicio = 0; inicio + perfil.Tamanho <= caracteres.Length; inicio++)
            {
                var numero = caracteres.Substring(inicio, perfil.Tamanho);

                if (!vistos.Add(numero))
                    continue;

                var validacao = perfil.Validador.Validar(numero);

                candidatos.Add(new CandidatoDocumento(
                    numero,
                    validacao.Valido ? perfil.Validador.Formatar(numero) : numero,
                    validacao,
                    Contexto(texto, ocorrencia.Index, ocorrencia.Length),
                    TemRotuloPerto(textoNormalizado, ocorrencia.Index, perfil),
                    isolado));
            }
        }

        var ordenados = candidatos
            .OrderByDescending(candidato => candidato.Confianca)
            .ToList();

        // Todos os válidos interessam; dos recusados, só uma amostra — o resto é ruído do
        // recorte e só atrapalha quem vai conferir na tela.
        return
        [
            .. ordenados.Where(candidato => candidato.Validacao.Valido),
            .. ordenados.Where(candidato => !candidato.Validacao.Valido).Take(TetoDeCandidatosInvalidos)
        ];
    }

    private static string Contexto(string texto, int indice, int tamanho)
    {
        const int Margem = 40;

        var inicio = Math.Max(0, indice - Margem);
        var fim = Math.Min(texto.Length, indice + tamanho + Margem);

        return texto[inicio..fim].Replace('\n', ' ').Replace('\r', ' ').Trim();
    }

    private static bool TemRotuloPerto(string textoMaiusculo, int indice, PerfilDeLeitura perfil)
    {
        const int Janela = 45;

        var inicio = Math.Max(0, indice - Janela);
        var trecho = textoMaiusculo[inicio..Math.Min(textoMaiusculo.Length, indice + Janela)];

        return perfil.TemRotulo(trecho);
    }

    private void GravarDiagnostico(byte[] imagem, float inclinacao)
    {
        if (string.IsNullOrWhiteSpace(PastaDeDiagnostico))
            return;

        Directory.CreateDirectory(PastaDeDiagnostico);

        var nome = $"preparada_{DateTime.Now:HHmmss_fff}_incl{inclinacao:+0;-0;0}.png";
        File.WriteAllBytes(Path.Combine(PastaDeDiagnostico, nome), imagem);
    }

    /// <summary>
    /// Correções de inclinação tentadas, em ordem, somadas à orientação da página. A primeira é
    /// "não mexer": a maioria dos arquivos é reta e não deve pagar o custo das outras passadas.
    /// </summary>
    private static readonly float[] Inclinacoes = [0f, -5f, 5f];

    /// <summary>Largura mínima em que o Tesseract ainda lê o número de registro com alguma chance.</summary>
    private const int LarguraMinima = 1600;

    /// <summary>
    /// Prepara a imagem para o OCR: amplia o que veio pequeno, converte para cinza com
    /// contraste esticado e, se pedido, corrige a inclinação.
    /// </summary>
    /// <remarks>
    /// Medido numa bateria de degradações de foto: sem esta etapa, imagem reduzida,
    /// com pouco contraste ou com sombra simplesmente não produzem leitura.
    /// </remarks>
    private static byte[] Preparar(byte[] imagem, float inclinacao, int? larguraMaxima = null)
    {
        using var original = SKBitmap.Decode(imagem);

        if (original is null)
            return imagem;

        var escala = larguraMaxima is { } teto
            ? Math.Min(1f, (float)teto / Math.Max(original.Width, original.Height))
            : original.Width < LarguraMinima
                ? Math.Min(3f, (float)LarguraMinima / original.Width)
                : 1f;

        var largura = (int)(original.Width * escala);
        var altura = (int)(original.Height * escala);

        // A tela cresce para caber a página girada inteira; num quarto de volta, largura e
        // altura simplesmente trocam de lugar.
        var radianos = inclinacao * MathF.PI / 180f;
        var cosseno = MathF.Abs(MathF.Round(MathF.Cos(radianos), 6));
        var seno = MathF.Abs(MathF.Round(MathF.Sin(radianos), 6));
        var larguraFinal = (int)MathF.Ceiling(largura * cosseno + altura * seno);
        var alturaFinal = (int)MathF.Ceiling(largura * seno + altura * cosseno);

        using var destino = new SKBitmap(larguraFinal, alturaFinal, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(destino);

        canvas.Clear(SKColors.White);

        if (inclinacao != 0f)
        {
            canvas.Translate(larguraFinal / 2f, alturaFinal / 2f);
            canvas.RotateDegrees(inclinacao);
            canvas.Translate(-largura / 2f, -altura / 2f);
        }

        using var tinta = new SKPaint
        {
            IsAntialias = true,
            ColorFilter = SKColorFilter.CreateColorMatrix(CinzaComContraste)
        };

        var amostragem = new SKSamplingOptions(SKCubicResampler.Mitchell);

        canvas.DrawBitmap(original, new SKRect(0, 0, largura, altura), amostragem, tinta);
        canvas.Flush();

        using var codificada = destino.Encode(SKEncodedImageFormat.Png, 100);
        return codificada.ToArray();
    }

    /// <summary>
    /// Converte para tons de cinza (pesos de luminância) e estica o contraste em 1,7×
    /// em torno do meio da faixa, que é o que separa o texto do fundo guilhochado.
    /// </summary>
    private static readonly float[] CinzaComContraste = MatrizCinzaComContraste(1.7f);

    private static float[] MatrizCinzaComContraste(float contraste)
    {
        // Luminância perceptual, já multiplicada pelo ganho de contraste.
        const float PesoVermelho = 0.299f;
        const float PesoVerde = 0.587f;
        const float PesoAzul = 0.114f;

        // A coluna de translação da matriz do SkiaSharp está na escala 0..1, não 0..255.
        // Multiplicar por 255 aqui produz uma imagem inteiramente preta, e o OCR devolve
        // vazio sem nenhum erro — foi assim que o bug passou despercebido até a imagem
        // preparada ser gravada em disco.
        var deslocamento = (1f - contraste) / 2f;

        var vermelho = PesoVermelho * contraste;
        var verde = PesoVerde * contraste;
        var azul = PesoAzul * contraste;

        return
        [
            vermelho, verde, azul, 0, deslocamento,
            vermelho, verde, azul, 0, deslocamento,
            vermelho, verde, azul, 0, deslocamento,
            0,        0,     0,    1, 0
        ];
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    private static List<byte[]> RenderizarPdf(byte[] pdf, int maximoDePaginas)
    {
        var paginas = new List<byte[]>();
        var total = PDFtoImage.Conversion.GetPageCount(pdf);
        var tamanhos = PDFtoImage.Conversion.GetPageSizes(pdf);

        for (var indice = 0; indice < Math.Min(total, maximoDePaginas); indice++)
        {
            using var bitmap = PDFtoImage.Conversion.ToImage(
                pdf,
                page: indice,
                options: new PDFtoImage.RenderOptions(Dpi: DpiDaPagina(tamanhos[indice])));

            using var dados = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            paginas.Add(dados.ToArray());
        }

        return paginas;
    }

    /// <summary>Maior lado, em pixels, de uma página de PDF renderizada.</summary>
    private const int LadoMaximoRenderizado = 3500;

    /// <summary>
    /// 300 DPI é o mínimo em que o Tesseract lê texto pequeno de documento A4 com alguma chance.
    /// Mas foto de celular salva em PDF costuma declarar a página no tamanho da foto a 72 ppi
    /// (1487 × 2286 pt, medido), e 300 DPI ali vira 6200 × 9500 px: o OCR leva minutos e lê
    /// o fundo guilhochado como texto. O teto no maior lado mantém a página no tamanho útil.
    /// </summary>
    private static int DpiDaPagina(System.Drawing.SizeF tamanhoEmPontos)
    {
        var maiorLado = Math.Max(tamanhoEmPontos.Width, tamanhoEmPontos.Height);

        if (maiorLado <= 0)
            return 300;

        return (int)Math.Clamp(LadoMaximoRenderizado * 72f / maiorLado, 72f, 300f);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_descartado)
            return;

        _engine.Dispose();
        _descartado = true;
    }
}

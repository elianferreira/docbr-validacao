namespace DocBr.Validacao;

/// <summary>
/// Valida a matrícula de certidão de registro civil — nascimento, casamento ou óbito —
/// no formato de 32 dígitos do Provimento 63 do CNJ.
/// </summary>
/// <remarks>
/// A matrícula é a mesma para os três tipos de certidão; o que distingue é o campo de
/// tipo de livro, na posição 15. O layout é:
/// <code>
/// CCCCCC DD EE AAAA T LLLLL FFF NNNNNNN VV
/// |      |  |  |    | |     |   |       └ dígitos verificadores
/// |      |  |  |    | |     |   └ número do termo
/// |      |  |  |    | |     └ número da folha
/// |      |  |  |    | └ número do livro
/// |      |  |  |    └ tipo do livro (1 nascimento, 2 casamento, 3 casamento religioso,
/// |      |  |  |      4 óbito, 5 natimorto, 6 registros diversos)
/// |      |  |  └ ano do registro
/// |      |  └ unidade federativa
/// |      └ tipo de serventia
/// └ código do cartório (CNS)
/// </code>
/// Os dois verificadores usam módulo 11 com pesos cíclicos de 0 a 10: o multiplicador
/// começa em <c>32 menos o tamanho do trecho</c>, cresce de um em um e volta a 0 depois
/// do 10. Resultado maior que 9 vira 1.
/// <para>Algoritmo conferido contra a biblioteca de referência validate-docbr.</para>
/// <para>
/// Passar aqui prova que a matrícula é bem formada. Se a certidão existe, e se continua
/// valendo (uma certidão de casamento pode ter averbação de divórcio), só o cartório
/// emissor ou a Central de Informações do Registro Civil respondem.
/// </para>
/// </remarks>
public sealed class ValidadorCertidao : IValidadorDocumento
{
    /// <summary>Tipos de livro conhecidos, pela posição 15 da matrícula.</summary>
    private static readonly Dictionary<char, string> TiposDeLivro = new()
    {
        ['1'] = "nascimento",
        ['2'] = "casamento",
        ['3'] = "casamento religioso com efeito civil",
        ['4'] = "óbito",
        ['5'] = "natimorto",
        ['6'] = "registros diversos"
    };

    /// <inheritdoc />
    public string Slug => "certidao";

    /// <inheritdoc />
    public string Nome => "Certidão de registro civil";

    /// <inheritdoc />
    public string Descricao =>
        "Matrícula de 32 dígitos de certidão de nascimento, casamento ou óbito (Provimento 63 do CNJ). " +
        "Confere a estrutura; não diz se a certidão existe nem se houve averbação.";

    /// <inheritdoc />
    public int Tamanho => 32;

    /// <inheritdoc />
    public ResultadoValidacao Validar(string? documento)
    {
        var preparo = EntradaNumerica.Preparar(documento, Nome, Tamanho, ".-");

        if (preparo.Falhou)
            return preparo.Resultado;

        var digitos = preparo.Digitos;
        var esperados = DigitosVerificadores(digitos[..30]);

        if (digitos[30] != esperados[0] || digitos[31] != esperados[1])
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.DigitoVerificador,
                $"Dígitos verificadores não conferem: informados '{digitos[30..]}', esperados '{esperados}'.",
                digitos);
        }

        return ResultadoValidacao.Ok(digitos);
    }

    /// <summary>
    /// Devolve o tipo de certidão indicado pela matrícula ("casamento", "nascimento", …),
    /// ou <c>null</c> quando a posição do tipo de livro tem um valor desconhecido.
    /// </summary>
    /// <remarks>
    /// Serve para conferir se a matrícula apresentada é mesmo do tipo de certidão esperado —
    /// alguém pode entregar uma certidão de nascimento quando o processo pede a de casamento.
    /// </remarks>
    public string? TipoDeCertidao(string? documento)
    {
        var digitos = Digitos.Somente(documento ?? string.Empty);

        if (digitos.Length != Tamanho)
            return null;

        return TiposDeLivro.TryGetValue(digitos[14], out var tipo) ? tipo : null;
    }

    /// <inheritdoc />
    public string Formatar(string? documento)
    {
        var d = EntradaNumerica.Exigir(documento, Nome, Tamanho);

        return $"{d[..6]}.{d[6..8]}.{d[8..10]}.{d[10..14]}.{d[14]}.{d[15..20]}.{d[20..23]}.{d[23..30]}-{d[30..]}";
    }

    /// <inheritdoc />
    public string Gerar(bool comMascara = false) => Gerar(comMascara, tipoDeLivro: null);

    /// <summary>Gera uma matrícula válida, opcionalmente fixando o tipo de livro.</summary>
    /// <param name="comMascara">Aplica a máscara de exibição.</param>
    /// <param name="tipoDeLivro">
    /// Dígito do tipo de livro (por exemplo <c>'2'</c> para casamento). Quando omitido, é sorteado.
    /// </param>
    public string Gerar(bool comMascara, char? tipoDeLivro)
    {
        var corpo = EntradaNumerica.CorpoAleatorio(30).ToCharArray();

        corpo[14] = tipoDeLivro ?? Digitos.Algarismo(Random.Shared.Next(1, 7));

        var certidao = new string(corpo);
        certidao += DigitosVerificadores(certidao);

        return comMascara ? Formatar(certidao) : certidao;
    }

    private static string DigitosVerificadores(string corpo)
    {
        var primeiro = SomaPonderada(corpo) % 11;

        if (primeiro > 9)
            primeiro = 1;

        var segundo = SomaPonderada(corpo + Digitos.Algarismo(primeiro)) % 11;

        if (segundo > 9)
            segundo = 1;

        return $"{primeiro}{segundo}";
    }

    private static int SomaPonderada(string trecho)
    {
        var total = 0;
        var multiplicador = 32 - trecho.Length;

        foreach (var algarismo in trecho)
        {
            total += Digitos.Valor(algarismo) * multiplicador;
            multiplicador++;

            // Os pesos são cíclicos de 0 a 10.
            if (multiplicador > 10)
                multiplicador = 0;
        }

        return total;
    }
}

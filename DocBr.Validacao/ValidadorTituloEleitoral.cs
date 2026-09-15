namespace DocBr.Validacao;

/// <summary>Valida o título de eleitor (12 dígitos).</summary>
/// <remarks>
/// Os 8 primeiros dígitos são o número sequencial, os dois seguintes identificam a unidade
/// federativa (01 a 28) e os dois últimos são verificadores.
/// <list type="bullet">
///   <item>primeiro verificador: sequencial com pesos 2..9, módulo 11, resto 10 vira 0;</item>
///   <item>segundo verificador: os dois dígitos da UF mais o primeiro verificador, com
///   pesos 7, 8 e 9, módulo 11, resto 10 vira 0.</item>
/// </list>
/// <para>Algoritmo conferido contra a biblioteca de referência validate-docbr.</para>
/// </remarks>
public sealed class ValidadorTituloEleitoral : IValidadorDocumento
{
    /// <summary>
    /// Códigos de unidade federativa aceitos: 01 a 28 (27 unidades mais o código do exterior).
    /// A referência gera de 01 a 18, mas validar apenas essa faixa recusaria títulos legítimos.
    /// </summary>
    private const int UnidadeFederativaMinima = 1;
    private const int UnidadeFederativaMaxima = 28;

    /// <inheritdoc />
    public string Slug => "titulo-eleitor";

    /// <inheritdoc />
    public string Nome => "Título de eleitor";

    /// <inheritdoc />
    public string Descricao =>
        "Título de eleitor, 12 dígitos, sendo dois deles o código da unidade federativa. " +
        "Confere a estrutura; não consulta a Justiça Eleitoral.";

    /// <inheritdoc />
    public int Tamanho => 12;

    /// <inheritdoc />
    public ResultadoValidacao Validar(string? documento)
    {
        var preparo = EntradaNumerica.Preparar(documento, Nome, Tamanho, ".-");

        if (preparo.Falhou)
            return preparo.Resultado;

        var digitos = preparo.Digitos;

        var unidadeFederativa = Digitos.Valor(digitos[8]) * 10 + Digitos.Valor(digitos[9]);

        if (unidadeFederativa is < UnidadeFederativaMinima or > UnidadeFederativaMaxima)
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.ValorForaDoDominio,
                $"O código da unidade federativa deve ficar entre " +
                $"{UnidadeFederativaMinima:00} e {UnidadeFederativaMaxima:00}; informado '{digitos[8]}{digitos[9]}'.",
                digitos);
        }

        var primeiro = PrimeiroDigito(digitos);
        var segundo = SegundoDigito(digitos, primeiro);

        if (digitos[10] != primeiro || digitos[11] != segundo)
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.DigitoVerificador,
                $"Dígitos verificadores não conferem: informados '{digitos[10]}{digitos[11]}', esperados '{primeiro}{segundo}'.",
                digitos);
        }

        return ResultadoValidacao.Ok(digitos);
    }

    /// <inheritdoc />
    public string Formatar(string? documento)
    {
        var digitos = EntradaNumerica.Exigir(documento, Nome, Tamanho);
        return $"{digitos[..4]} {digitos[4..8]} {digitos[8..]}";
    }

    /// <inheritdoc />
    public string Gerar(bool comMascara = false)
    {
        var sequencial = EntradaNumerica.CorpoAleatorio(8);
        var unidadeFederativa = Random.Shared
            .Next(UnidadeFederativaMinima, UnidadeFederativaMaxima + 1)
            .ToString("00");

        var corpo = sequencial + unidadeFederativa;

        var primeiro = PrimeiroDigito(corpo);
        var segundo = SegundoDigito(corpo, primeiro);

        var titulo = corpo + primeiro + segundo;

        return comMascara ? Formatar(titulo) : titulo;
    }

    private static char PrimeiroDigito(string digitos)
    {
        var total = 0;

        for (var posicao = 0; posicao < 8; posicao++)
            total += Digitos.Valor(digitos[posicao]) * (posicao + 2);

        var resto = total % 11;

        return Digitos.Algarismo(resto == 10 ? 0 : resto);
    }

    private static char SegundoDigito(string digitos, char primeiroDigito)
    {
        var total =
            Digitos.Valor(digitos[8]) * 7 +
            Digitos.Valor(digitos[9]) * 8 +
            Digitos.Valor(primeiroDigito) * 9;

        var resto = total % 11;

        return Digitos.Algarismo(resto == 10 ? 0 : resto);
    }
}

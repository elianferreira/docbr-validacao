namespace DocBr.Validacao;

/// <summary>Valida o PIS/PASEP/NIT (11 dígitos, um dígito verificador).</summary>
/// <remarks>
/// Soma dos 10 primeiros algarismos com os pesos fixos 3, 2, 9, 8, 7, 6, 5, 4, 3, 2;
/// resto da divisão por 11; resto menor que 2 resulta em dígito 0, senão 11 menos o resto.
/// <para>Algoritmo conferido contra a biblioteca de referência validate-docbr.</para>
/// </remarks>
public sealed class ValidadorPis : IValidadorDocumento
{
    private static readonly int[] Pesos = [3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <inheritdoc />
    public string Slug => "pis";

    /// <inheritdoc />
    public string Nome => "PIS/PASEP";

    /// <inheritdoc />
    public string Descricao =>
        "PIS, PASEP ou NIT, 11 dígitos com um dígito verificador. " +
        "Confere a estrutura; não consulta a base do trabalhador.";

    /// <inheritdoc />
    public int Tamanho => 11;

    /// <inheritdoc />
    public ResultadoValidacao Validar(string? documento)
    {
        var preparo = EntradaNumerica.Preparar(documento, Nome, Tamanho, ".-");

        if (preparo.Falhou)
            return preparo.Resultado;

        var digitos = preparo.Digitos;

        if (Digitos.TodosIguais(digitos))
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.DigitosRepetidos,
                $"O {Nome} não pode ser formado por um único dígito repetido.",
                digitos);
        }

        var esperado = Digito(digitos);

        if (digitos[10] != esperado)
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.DigitoVerificador,
                $"Dígito verificador não confere: informado '{digitos[10]}', esperado '{esperado}'.",
                digitos);
        }

        return ResultadoValidacao.Ok(digitos);
    }

    /// <inheritdoc />
    public string Formatar(string? documento)
    {
        var digitos = EntradaNumerica.Exigir(documento, Nome, Tamanho);
        return $"{digitos[..3]}.{digitos[3..8]}.{digitos[8..10]}-{digitos[10..]}";
    }

    /// <inheritdoc />
    public string Gerar(bool comMascara = false)
    {
        string corpo;

        do
        {
            corpo = EntradaNumerica.CorpoAleatorio(10);
        }
        while (Digitos.TodosIguais(corpo));

        var pis = corpo + Digito(corpo);

        return comMascara ? Formatar(pis) : pis;
    }

    private static char Digito(string digitos)
    {
        var total = 0;

        for (var posicao = 0; posicao < Pesos.Length; posicao++)
            total += Digitos.Valor(digitos[posicao]) * Pesos[posicao];

        var resto = total % 11;

        return Digitos.Algarismo(resto < 2 ? 0 : 11 - resto);
    }
}

namespace DocBr.Validacao;

/// <summary>Valida o RENAVAM (11 dígitos, um dígito verificador).</summary>
/// <remarks>
/// Soma dos 10 primeiros algarismos com os pesos 3, 2, 9, 8, 7, 6, 5, 4, 3, 2; a soma é
/// multiplicada por 10 e tirado o resto por 11; resto 10 vira 0.
/// <para>Algoritmo conferido contra a biblioteca de referência validate-docbr.</para>
/// <para>
/// Diferente do CPF e do PIS, o RENAVAM não recusa sequências de dígitos repetidos: a
/// referência não faz essa checagem, e adicioná-la por conta própria criaria divergência.
/// </para>
/// </remarks>
public sealed class ValidadorRenavam : IValidadorDocumento
{
    private static readonly int[] Pesos = [3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    /// <inheritdoc />
    public string Slug => "renavam";

    /// <inheritdoc />
    public string Nome => "RENAVAM";

    /// <inheritdoc />
    public string Descricao =>
        "Registro Nacional de Veículos Automotores, 11 dígitos. " +
        "Confere a estrutura; não consulta a base do veículo.";

    /// <inheritdoc />
    public int Tamanho => 11;

    /// <inheritdoc />
    public ResultadoValidacao Validar(string? documento)
    {
        var preparo = EntradaNumerica.Preparar(documento, Nome, Tamanho, "-");

        if (preparo.Falhou)
            return preparo.Resultado;

        var digitos = preparo.Digitos;
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
        return $"{digitos[..10]}-{digitos[10]}";
    }

    /// <inheritdoc />
    public string Gerar(bool comMascara = false)
    {
        var corpo = EntradaNumerica.CorpoAleatorio(10);
        var renavam = corpo + Digito(corpo);

        return comMascara ? Formatar(renavam) : renavam;
    }

    private static char Digito(string digitos)
    {
        var total = 0;

        for (var posicao = 0; posicao < Pesos.Length; posicao++)
            total += Digitos.Valor(digitos[posicao]) * Pesos[posicao];

        var resto = total * 10 % 11;

        return Digitos.Algarismo(resto == 10 ? 0 : resto);
    }
}

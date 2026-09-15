namespace DocBr.Validacao;

/// <summary>Valida o Cadastro de Pessoas Físicas (11 dígitos).</summary>
/// <remarks>
/// Dois dígitos verificadores módulo 11: o primeiro sobre os 9 primeiros algarismos com
/// pesos 10..2, o segundo sobre os 10 primeiros com pesos 11..2. Em ambos, a soma é
/// multiplicada por 10 antes do resto, e o resto 10 vira 0.
/// <para>Algoritmo conferido contra a biblioteca de referência validate-docbr.</para>
/// </remarks>
public sealed class ValidadorCpf : IValidadorDocumento
{
    /// <inheritdoc />
    public string Slug => "cpf";

    /// <inheritdoc />
    public string Nome => "CPF";

    /// <inheritdoc />
    public string Descricao =>
        "Cadastro de Pessoas Físicas, 11 dígitos. Confere a estrutura; " +
        "não diz se o CPF existe nem se está regular na Receita Federal.";

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

        var primeiro = Digito(digitos, ateOndeInclusive: 9, pesoInicial: 10);
        var segundo = Digito(digitos, ateOndeInclusive: 10, pesoInicial: 11);

        if (digitos[9] != primeiro || digitos[10] != segundo)
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.DigitoVerificador,
                $"Dígitos verificadores não conferem: informados '{digitos[9]}{digitos[10]}', esperados '{primeiro}{segundo}'.",
                digitos);
        }

        return ResultadoValidacao.Ok(digitos);
    }

    /// <inheritdoc />
    public string Formatar(string? documento)
    {
        var digitos = EntradaNumerica.Exigir(documento, Nome, Tamanho);
        return $"{digitos[..3]}.{digitos[3..6]}.{digitos[6..9]}-{digitos[9..]}";
    }

    /// <inheritdoc />
    public string Gerar(bool comMascara = false)
    {
        string corpo;

        do
        {
            corpo = EntradaNumerica.CorpoAleatorio(9);
        }
        while (Digitos.TodosIguais(corpo));

        var comPrimeiro = corpo + Digito(corpo, ateOndeInclusive: 9, pesoInicial: 10);
        var cpf = comPrimeiro + Digito(comPrimeiro, ateOndeInclusive: 10, pesoInicial: 11);

        return comMascara ? Formatar(cpf) : cpf;
    }

    private static char Digito(string digitos, int ateOndeInclusive, int pesoInicial)
    {
        var total = 0;
        var peso = pesoInicial;

        for (var posicao = 0; posicao < ateOndeInclusive; posicao++)
        {
            total += Digitos.Valor(digitos[posicao]) * peso;
            peso--;
        }

        var resto = total * 10 % 11;

        return Digitos.Algarismo(resto == 10 ? 0 : resto);
    }
}

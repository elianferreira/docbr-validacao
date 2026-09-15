namespace DocBr.Validacao;

/// <summary>Valida o Cartão Nacional de Saúde (15 dígitos).</summary>
/// <remarks>
/// O CNS tem duas regras distintas, escolhidas pelo primeiro dígito:
/// <list type="bullet">
///   <item><b>1 ou 2</b> (cartão definitivo): os 11 primeiros dígitos são o número do
///   usuário; os quatro últimos são um sufixo calculado — <c>000</c> ou <c>001</c> seguido
///   do dígito verificador. A validação recalcula o sufixo e compara.</item>
///   <item><b>7, 8 ou 9</b> (cartão provisório): não há sufixo calculado; exige-se apenas
///   que a soma dos 15 dígitos com pesos 15..1 seja múltiplo de 11.</item>
/// </list>
/// Qualquer outro primeiro dígito não corresponde a um CNS.
/// <para>Algoritmo conferido contra a biblioteca de referência validate-docbr.</para>
/// </remarks>
public sealed class ValidadorCns : IValidadorDocumento
{
    private static readonly char[] PrimeirosDigitosValidos = ['1', '2', '7', '8', '9'];

    /// <inheritdoc />
    public string Slug => "cns";

    /// <inheritdoc />
    public string Nome => "CNS";

    /// <inheritdoc />
    public string Descricao =>
        "Cartão Nacional de Saúde, 15 dígitos. Confere a estrutura; " +
        "não consulta o cadastro do SUS.";

    /// <inheritdoc />
    public int Tamanho => 15;

    /// <inheritdoc />
    public ResultadoValidacao Validar(string? documento)
    {
        var preparo = EntradaNumerica.Preparar(documento, Nome, Tamanho, ".-");

        if (preparo.Falhou)
            return preparo.Resultado;

        var digitos = preparo.Digitos;

        if (!PrimeirosDigitosValidos.Contains(digitos[0]))
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.ValorForaDoDominio,
                $"O primeiro dígito do {Nome} deve ser 1, 2, 7, 8 ou 9; informado '{digitos[0]}'.",
                digitos);
        }

        if (digitos[0] is '1' or '2')
        {
            var esperado = MontarDefinitivo(digitos[..11]);

            if (esperado != digitos)
            {
                return ResultadoValidacao.Falha(
                    MotivoInvalidez.DigitoVerificador,
                    $"Os quatro últimos dígitos não conferem: informados '{digitos[11..]}', esperados '{esperado[11..]}'.",
                    digitos);
            }

            return ResultadoValidacao.Ok(digitos);
        }

        if (SomaPonderada(digitos, digitos.Length) % 11 != 0)
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.DigitoVerificador,
                $"A soma ponderada de um {Nome} provisório precisa ser múltiplo de 11, e não é.",
                digitos);
        }

        return ResultadoValidacao.Ok(digitos);
    }

    /// <inheritdoc />
    public string Formatar(string? documento)
    {
        var digitos = EntradaNumerica.Exigir(documento, Nome, Tamanho);
        return $"{digitos[..3]} {digitos[3..7]} {digitos[7..11]} {digitos[11..]}";
    }

    /// <inheritdoc />
    public string Gerar(bool comMascara = false)
    {
        // Só o caminho definitivo é gerado: o provisório exigiria ajustar dígitos até a soma
        // fechar em múltiplo de 11, o que produz números menos representativos.
        var inicio = Random.Shared.Next(2) == 0 ? '1' : '2';
        var cns = MontarDefinitivo(inicio + EntradaNumerica.CorpoAleatorio(10));

        return comMascara ? Formatar(cns) : cns;
    }

    /// <summary>Monta o CNS definitivo completo a partir dos 11 dígitos do número do usuário.</summary>
    private static string MontarDefinitivo(string onzePrimeiros)
    {
        var total = SomaPonderada(onzePrimeiros, 11);
        var verificador = 11 - total % 11;

        if (verificador == 11)
            verificador = 0;

        if (verificador != 10)
            return onzePrimeiros + "000" + Digitos.Algarismo(verificador);

        // Verificador 10 não cabe em uma casa: soma-se 2 ao total e o sufixo passa a ser 001.
        verificador = 11 - (total + 2) % 11;

        return onzePrimeiros + "001" + Digitos.Algarismo(verificador);
    }

    private static int SomaPonderada(string digitos, int quantidade)
    {
        var total = 0;

        for (var posicao = 0; posicao < quantidade; posicao++)
            total += Digitos.Valor(digitos[posicao]) * (15 - posicao);

        return total;
    }
}

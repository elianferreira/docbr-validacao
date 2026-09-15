using System.Text;

namespace DocBr.Validacao;

/// <summary>
/// Valida a Carteira Nacional de Habilitação (número de registro, 11 dígitos).
/// </summary>
/// <remarks>
/// O algoritmo é o mesmo da biblioteca de referência validate-docbr
/// (https://github.com/alvarofpp/validate-docbr), reimplementado em C#:
/// <list type="number">
///   <item>primeiro dígito: soma dos 9 primeiros dígitos com pesos 9..1, módulo 11;
///   se o resto for maior ou igual a 10 o dígito é 0 e um desconto de 2 passa a valer para o segundo;</item>
///   <item>segundo dígito: soma dos 9 primeiros dígitos com pesos 1..9, módulo 11, menos o desconto;
///   se ficar negativo soma-se 11, e se ficar maior ou igual a 10 o dígito é 0.</item>
/// </list>
/// Vários trechos que circulam na internet erram justamente o tratamento do desconto
/// (aplicam o corte em 10 antes de subtrair, e não tratam o resultado negativo),
/// o que recusa CNHs legítimas. Os testes cobrem esses dois casos de borda.
/// <para>
/// Esta validação é estrutural: prova que o número é bem formado, não que a habilitação
/// exista ou esteja vigente — isso só o Senatran responde.
/// </para>
/// </remarks>
public sealed class ValidadorCnh : IValidadorDocumento
{
    /// <inheritdoc />
    public string Slug => "cnh";

    /// <inheritdoc />
    public string Nome => "CNH";

    /// <inheritdoc />
    public string Descricao =>
        "Número de registro da Carteira Nacional de Habilitação, 11 dígitos. " +
        "Não prova que a habilitação existe nem que está vigente.";

    /// <inheritdoc />
    public int Tamanho => 11;

    /// <inheritdoc />
    public ResultadoValidacao Validar(string? documento)
    {
        if (string.IsNullOrWhiteSpace(documento))
            return ResultadoValidacao.Falha(MotivoInvalidez.EntradaVazia, "Informe o número da CNH.");

        foreach (var caractere in documento)
        {
            if (!char.IsDigit(caractere) && caractere != ' ' && caractere != '.' && caractere != '-')
            {
                return ResultadoValidacao.Falha(
                    MotivoInvalidez.CaractereInvalido,
                    $"A CNH aceita apenas dígitos e os separadores ' ', '.' e '-'; encontrado '{caractere}'.");
            }
        }

        var digitos = SomenteDigitos(documento);

        if (digitos.Length != Tamanho)
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.QuantidadeDeDigitos,
                $"A CNH deve ter {Tamanho} dígitos; foram informados {digitos.Length}.",
                digitos);
        }

        if (TodosOsDigitosIguais(digitos))
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.DigitosRepetidos,
                "A CNH não pode ser formada por um único dígito repetido.",
                digitos);
        }

        var primeiro = PrimeiroDigito(digitos, out var desconto);
        var segundo = SegundoDigito(digitos, desconto);

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
        var digitos = SomenteDigitos(documento ?? string.Empty);

        if (digitos.Length != Tamanho)
            throw new ArgumentException($"A CNH precisa de {Tamanho} dígitos para ser formatada.", nameof(documento));

        return $"{digitos[..3]} {digitos[3..6]} {digitos[6..9]} {digitos[9..]}";
    }

    /// <inheritdoc />
    public string Gerar(bool comMascara = false)
    {
        var digitos = new char[Tamanho];

        for (var i = 0; i < 9; i++)
            digitos[i] = (char)('0' + Random.Shared.Next(10));

        var corpo = new string(digitos, 0, 9);

        // Um corpo com os nove dígitos iguais geraria uma CNH que a própria validação recusa.
        if (TodosOsDigitosIguais(corpo))
            return Gerar(comMascara);

        digitos[9] = PrimeiroDigito(corpo, out var desconto);
        digitos[10] = SegundoDigito(corpo, desconto);

        var cnh = new string(digitos);
        return comMascara ? Formatar(cnh) : cnh;
    }

    private static char PrimeiroDigito(string digitos, out int desconto)
    {
        desconto = 0;
        var total = 0;

        for (var peso = 9; peso >= 1; peso--)
            total += (digitos[9 - peso] - '0') * peso;

        var valor = total % 11;

        if (valor >= 10)
        {
            valor = 0;
            desconto = 2;
        }

        return (char)('0' + valor);
    }

    private static char SegundoDigito(string digitos, int desconto)
    {
        var total = 0;

        for (var peso = 1; peso <= 9; peso++)
            total += (digitos[peso - 1] - '0') * peso;

        var valor = total % 11 - desconto;

        if (valor < 0)
            valor += 11;

        if (valor >= 10)
            valor = 0;

        return (char)('0' + valor);
    }

    private static string SomenteDigitos(string entrada)
    {
        var builder = new StringBuilder(entrada.Length);

        foreach (var caractere in entrada)
        {
            if (char.IsDigit(caractere))
                builder.Append(caractere);
        }

        return builder.ToString();
    }

    private static bool TodosOsDigitosIguais(string digitos)
    {
        for (var i = 1; i < digitos.Length; i++)
        {
            if (digitos[i] != digitos[0])
                return false;
        }

        return digitos.Length > 0;
    }
}

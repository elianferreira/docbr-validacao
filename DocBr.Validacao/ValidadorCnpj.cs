namespace DocBr.Validacao;

/// <summary>
/// Valida o Cadastro Nacional da Pessoa Jurídica (14 caracteres), incluindo o
/// formato alfanumérico que passa a conviver com o numérico.
/// </summary>
/// <remarks>
/// Os 12 primeiros caracteres podem ser letras ou algarismos; os 2 últimos são sempre
/// algarismos. O valor de cada caractere no cálculo é o seu código ASCII menos 48, o que
/// faz <c>'0'</c> valer 0 e <c>'A'</c> valer 17 — é assim que o CNPJ alfanumérico reaproveita
/// o mesmo módulo 11 do numérico.
/// <para>Algoritmo conferido contra a biblioteca de referência validate-docbr.</para>
/// </remarks>
public sealed class ValidadorCnpj : IValidadorDocumento
{
    private static readonly int[] PesosPrimeiro = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] PesosSegundo = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    private const string Alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    /// <inheritdoc />
    public string Slug => "cnpj";

    /// <inheritdoc />
    public string Nome => "CNPJ";

    /// <inheritdoc />
    public string Descricao =>
        "Cadastro Nacional da Pessoa Jurídica, 14 caracteres, numérico ou alfanumérico. " +
        "Confere a estrutura; não diz se a empresa existe ou está ativa.";

    /// <inheritdoc />
    public int Tamanho => 14;

    /// <inheritdoc />
    public ResultadoValidacao Validar(string? documento)
    {
        if (string.IsNullOrWhiteSpace(documento))
            return ResultadoValidacao.Falha(MotivoInvalidez.EntradaVazia, $"Informe o número do {Nome}.");

        var invalido = Digitos.PrimeiroCaractereInvalido(documento, "./- ", permiteLetras: true);

        if (invalido is not null)
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.CaractereInvalido,
                $"O {Nome} aceita letras, dígitos e os separadores '.', '/' e '-'; encontrado '{invalido}'.");
        }

        var caracteres = Digitos.SomenteAlfanumericos(documento);

        if (caracteres.Length != Tamanho)
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.QuantidadeDeDigitos,
                $"O {Nome} deve ter {Tamanho} caracteres; foram informados {caracteres.Length}.",
                caracteres);
        }

        if (Digitos.TodosIguais(caracteres))
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.DigitosRepetidos,
                $"O {Nome} não pode ser formado por um único caractere repetido.",
                caracteres);
        }

        // Os dois verificadores são sempre numéricos, mesmo no CNPJ alfanumérico.
        if (!char.IsDigit(caracteres[12]) || !char.IsDigit(caracteres[13]))
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.ValorForaDoDominio,
                $"Os dois últimos caracteres do {Nome} são dígitos verificadores e precisam ser algarismos.",
                caracteres);
        }

        var primeiro = Digito(caracteres, PesosPrimeiro);
        var segundo = Digito(caracteres, PesosSegundo);

        if (caracteres[12] != primeiro || caracteres[13] != segundo)
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.DigitoVerificador,
                $"Dígitos verificadores não conferem: informados '{caracteres[12]}{caracteres[13]}', esperados '{primeiro}{segundo}'.",
                caracteres);
        }

        return ResultadoValidacao.Ok(caracteres);
    }

    /// <inheritdoc />
    public string Formatar(string? documento)
    {
        var caracteres = Digitos.SomenteAlfanumericos(documento ?? string.Empty);

        if (caracteres.Length != Tamanho)
            throw new ArgumentException($"O {Nome} precisa de {Tamanho} caracteres para ser formatado.", nameof(documento));

        return $"{caracteres[..2]}.{caracteres[2..5]}.{caracteres[5..8]}/{caracteres[8..12]}-{caracteres[12..]}";
    }

    /// <inheritdoc />
    public string Gerar(bool comMascara = false) => Gerar(comMascara, alfanumerico: false);

    /// <summary>Gera um CNPJ válido, opcionalmente no formato alfanumérico.</summary>
    /// <param name="comMascara">Aplica a máscara de exibição.</param>
    /// <param name="alfanumerico">Sorteia letras além de algarismos nos 12 primeiros caracteres.</param>
    public string Gerar(bool comMascara, bool alfanumerico)
    {
        string corpo;

        do
        {
            var caracteres = new char[12];

            for (var i = 0; i < 12; i++)
            {
                caracteres[i] = alfanumerico
                    ? Alfabeto[Random.Shared.Next(Alfabeto.Length)]
                    : Digitos.Algarismo(Random.Shared.Next(10));
            }

            corpo = new string(caracteres);
        }
        while (Digitos.TodosIguais(corpo));

        var comPrimeiro = corpo + Digito(corpo, PesosPrimeiro);
        var cnpj = comPrimeiro + Digito(comPrimeiro, PesosSegundo);

        return comMascara ? Formatar(cnpj) : cnpj;
    }

    private static char Digito(string caracteres, int[] pesos)
    {
        var total = 0;

        for (var posicao = 0; posicao < pesos.Length; posicao++)
        {
            // Código ASCII menos 48: '0' vale 0, '9' vale 9, 'A' vale 17, 'Z' vale 42.
            total += (caracteres[posicao] - 48) * pesos[posicao];
        }

        var resto = total % 11;

        return Digitos.Algarismo(resto < 2 ? 0 : 11 - resto);
    }
}

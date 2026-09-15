namespace DocBr.Validacao;

/// <summary>
/// Valida o RG emitido pela SSP de São Paulo (8 dígitos mais um verificador, que pode ser 'X').
/// </summary>
/// <remarks>
/// <para>
/// <b>Leia antes de usar.</b> Não existe padrão nacional de RG. Cada secretaria de segurança
/// emite no seu próprio formato, com quantidade de dígitos diferente e, na maioria dos casos,
/// sem dígito verificador algum. Um "validador de RG" genérico é impossível: ele recusaria
/// documentos legítimos de outras unidades federativas.
/// </para>
/// <para>
/// Este validador cobre <b>somente</b> o RG de São Paulo, cujo verificador é público: soma
/// dos 8 dígitos com pesos 2..9, módulo 11; o verificador é <c>11 menos o resto</c>, com 10
/// representado por <c>'X'</c> e 11 por <c>0</c>.
/// </para>
/// <para>
/// Marcado como <see cref="Experimental"/>: diferente dos outros validadores deste projeto,
/// este algoritmo não tem uma implementação de referência independente contra a qual conferir —
/// a biblioteca validate-docbr não trata RG. Os testes provam consistência interna, não
/// correção contra terceiro.
/// </para>
/// </remarks>
public sealed class ValidadorRgSaoPaulo : IValidadorDocumento
{
    /// <inheritdoc />
    public string Slug => "rg-sp";

    /// <inheritdoc />
    public string Nome => "RG (SSP-SP)";

    /// <inheritdoc />
    public string Descricao =>
        "RG emitido em São Paulo, 9 caracteres com verificador que pode ser 'X'. " +
        "Não existe padrão nacional de RG: para outras unidades federativas, não use este validador.";

    /// <inheritdoc />
    public int Tamanho => 9;

    /// <inheritdoc />
    public bool Experimental => true;

    /// <inheritdoc />
    public ResultadoValidacao Validar(string? documento)
    {
        if (string.IsNullOrWhiteSpace(documento))
            return ResultadoValidacao.Falha(MotivoInvalidez.EntradaVazia, $"Informe o número do {Nome}.");

        var normalizado = Normalizar(documento);

        foreach (var caractere in documento)
        {
            var aceito = char.IsDigit(caractere)
                || caractere is '.' or '-' or ' '
                || char.ToUpperInvariant(caractere) == 'X';

            if (!aceito)
            {
                return ResultadoValidacao.Falha(
                    MotivoInvalidez.CaractereInvalido,
                    $"O {Nome} aceita dígitos, a letra 'X' no verificador e os separadores '.', '-' e espaço; encontrado '{caractere}'.");
            }
        }

        if (normalizado.Length != Tamanho)
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.QuantidadeDeDigitos,
                $"O {Nome} deve ter {Tamanho} caracteres; foram informados {normalizado.Length}.",
                normalizado);
        }

        var corpo = normalizado[..8];

        // O 'X' só faz sentido na casa do verificador.
        if (corpo.Any(caractere => !char.IsDigit(caractere)))
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.ValorForaDoDominio,
                $"Os oito primeiros caracteres do {Nome} precisam ser algarismos; o 'X' só vale no verificador.",
                normalizado);
        }

        if (Digitos.TodosIguais(normalizado))
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.DigitosRepetidos,
                $"O {Nome} não pode ser formado por um único caractere repetido.",
                normalizado);
        }

        var esperado = Verificador(corpo);

        if (normalizado[8] != esperado)
        {
            return ResultadoValidacao.Falha(
                MotivoInvalidez.DigitoVerificador,
                $"Dígito verificador não confere: informado '{normalizado[8]}', esperado '{esperado}'.",
                normalizado);
        }

        return ResultadoValidacao.Ok(normalizado);
    }

    /// <inheritdoc />
    public string Formatar(string? documento)
    {
        var normalizado = Normalizar(documento ?? string.Empty);

        if (normalizado.Length != Tamanho)
            throw new ArgumentException($"O {Nome} precisa de {Tamanho} caracteres para ser formatado.", nameof(documento));

        return $"{normalizado[..2]}.{normalizado[2..5]}.{normalizado[5..8]}-{normalizado[8]}";
    }

    /// <inheritdoc />
    public string Gerar(bool comMascara = false)
    {
        string corpo;

        do
        {
            corpo = EntradaNumerica.CorpoAleatorio(8);
        }
        while (Digitos.TodosIguais(corpo));

        var rg = corpo + Verificador(corpo);

        return comMascara ? Formatar(rg) : rg;
    }

    private static string Normalizar(string entrada) =>
        new(entrada
            .Where(caractere => char.IsDigit(caractere) || char.ToUpperInvariant(caractere) == 'X')
            .Select(char.ToUpperInvariant)
            .ToArray());

    private static char Verificador(string corpo)
    {
        var total = 0;

        for (var posicao = 0; posicao < 8; posicao++)
            total += Digitos.Valor(corpo[posicao]) * (posicao + 2);

        var verificador = 11 - total % 11;

        return verificador switch
        {
            10 => 'X',
            11 => '0',
            _ => Digitos.Algarismo(verificador)
        };
    }
}

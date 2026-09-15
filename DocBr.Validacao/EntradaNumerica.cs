namespace DocBr.Validacao;

/// <summary>
/// Passos de entrada repetidos por todos os validadores puramente numéricos:
/// recusar entrada vazia, recusar caractere estranho e conferir a quantidade de dígitos.
/// </summary>
internal readonly struct EntradaNumerica
{
    private EntradaNumerica(bool falhou, ResultadoValidacao resultado, string digitos)
    {
        Falhou = falhou;
        Resultado = resultado;
        Digitos = digitos;
    }

    /// <summary>Indica que a entrada já foi recusada e <see cref="Resultado"/> é a resposta final.</summary>
    public bool Falhou { get; }

    /// <summary>Resposta pronta quando <see cref="Falhou"/> é verdadeiro.</summary>
    public ResultadoValidacao Resultado { get; }

    /// <summary>Entrada sem máscara, disponível quando a checagem passou.</summary>
    public string Digitos { get; }

    /// <summary>Roda as três checagens de entrada comuns.</summary>
    /// <param name="documento">Texto informado pelo usuário.</param>
    /// <param name="nome">Nome do documento, usado nas mensagens.</param>
    /// <param name="tamanho">Quantidade de dígitos esperada.</param>
    /// <param name="separadores">Caracteres de máscara aceitos, além dos algarismos.</param>
    public static EntradaNumerica Preparar(string? documento, string nome, int tamanho, string separadores)
    {
        if (string.IsNullOrWhiteSpace(documento))
        {
            return Recusar(ResultadoValidacao.Falha(
                MotivoInvalidez.EntradaVazia, $"Informe o número do {nome}."));
        }

        var invalido = Validacao.Digitos.PrimeiroCaractereInvalido(documento, separadores + " ");

        if (invalido is not null)
        {
            var lista = string.Join(", ", separadores.Select(caractere => $"'{caractere}'"));

            return Recusar(ResultadoValidacao.Falha(
                MotivoInvalidez.CaractereInvalido,
                $"O {nome} aceita apenas dígitos e os separadores {lista}; encontrado '{invalido}'."));
        }

        var digitos = Validacao.Digitos.Somente(documento);

        if (digitos.Length != tamanho)
        {
            return Recusar(ResultadoValidacao.Falha(
                MotivoInvalidez.QuantidadeDeDigitos,
                $"O {nome} deve ter {tamanho} dígitos; foram informados {digitos.Length}.",
                digitos));
        }

        return new EntradaNumerica(false, default, digitos);
    }

    /// <summary>Extrai os dígitos exigindo o tamanho exato, para uso na formatação.</summary>
    /// <exception cref="ArgumentException">Quando a quantidade de dígitos não bate.</exception>
    public static string Exigir(string? documento, string nome, int tamanho)
    {
        var digitos = Validacao.Digitos.Somente(documento ?? string.Empty);

        if (digitos.Length != tamanho)
            throw new ArgumentException($"O {nome} precisa de {tamanho} dígitos para ser formatado.", nameof(documento));

        return digitos;
    }

    /// <summary>Sorteia um corpo de documento com a quantidade de algarismos pedida.</summary>
    public static string CorpoAleatorio(int quantidade)
    {
        var algarismos = new char[quantidade];

        for (var i = 0; i < quantidade; i++)
            algarismos[i] = Validacao.Digitos.Algarismo(Random.Shared.Next(10));

        return new string(algarismos);
    }

    private static EntradaNumerica Recusar(ResultadoValidacao resultado) =>
        new(true, resultado, resultado.Normalizado);
}

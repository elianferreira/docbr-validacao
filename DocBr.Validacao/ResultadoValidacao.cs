namespace DocBr.Validacao;

/// <summary>Motivo pelo qual um documento foi recusado.</summary>
public enum MotivoInvalidez
{
    /// <summary>O documento passou em todas as verificações.</summary>
    Nenhum = 0,

    /// <summary>Entrada nula, vazia ou só com espaços.</summary>
    EntradaVazia,

    /// <summary>A entrada contém caracteres que não fazem parte da máscara do documento.</summary>
    CaractereInvalido,

    /// <summary>A quantidade de dígitos não corresponde à esperada para o tipo.</summary>
    QuantidadeDeDigitos,

    /// <summary>Todos os dígitos são iguais (ex.: 11111111111).</summary>
    DigitosRepetidos,

    /// <summary>Os dígitos verificadores não conferem com o corpo do documento.</summary>
    DigitoVerificador,

    /// <summary>
    /// Um campo do documento tem valor fora do conjunto permitido — o primeiro dígito do CNS,
    /// ou o código da unidade federativa no título de eleitor, por exemplo.
    /// </summary>
    ValorForaDoDominio
}

/// <summary>Resultado de uma validação, com o motivo quando o documento é recusado.</summary>
/// <param name="Valido">Indica se o documento passou em todas as verificações.</param>
/// <param name="Motivo">Motivo da recusa; <see cref="MotivoInvalidez.Nenhum"/> quando válido.</param>
/// <param name="Descricao">Mensagem legível para exibição ou log.</param>
/// <param name="Normalizado">Documento sem máscara, apenas os dígitos, quando foi possível extraí-los.</param>
public readonly record struct ResultadoValidacao(
    bool Valido,
    MotivoInvalidez Motivo,
    string Descricao,
    string Normalizado)
{
    /// <summary>Cria um resultado de sucesso.</summary>
    public static ResultadoValidacao Ok(string normalizado) =>
        new(true, MotivoInvalidez.Nenhum, "Documento válido.", normalizado);

    /// <summary>Cria um resultado de recusa.</summary>
    public static ResultadoValidacao Falha(MotivoInvalidez motivo, string descricao, string normalizado = "") =>
        new(false, motivo, descricao, normalizado);
}

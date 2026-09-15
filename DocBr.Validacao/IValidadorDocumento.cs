namespace DocBr.Validacao;

/// <summary>Contrato comum a todos os validadores de documento brasileiro.</summary>
public interface IValidadorDocumento
{
    /// <summary>Identificador estável, em minúsculas e sem acento (ex.: <c>cnh</c>, <c>titulo-eleitor</c>).</summary>
    string Slug { get; }

    /// <summary>Nome do documento tratado por este validador (ex.: "CNH").</summary>
    string Nome { get; }

    /// <summary>Uma linha explicando o que este validador cobre e o que ele não prova.</summary>
    string Descricao { get; }

    /// <summary>Quantidade de caracteres do documento, já sem máscara.</summary>
    int Tamanho { get; }

    /// <summary>
    /// Indica que o algoritmo não tem uma implementação de referência independente contra a
    /// qual conferir, e por isso o resultado deve ser tratado com reserva.
    /// </summary>
    bool Experimental => false;

    /// <summary>Valida o documento, aceitando a entrada com ou sem máscara.</summary>
    ResultadoValidacao Validar(string? documento);

    /// <summary>Aplica a máscara de exibição do documento.</summary>
    /// <exception cref="ArgumentException">Quando a entrada não tem o tamanho esperado.</exception>
    string Formatar(string? documento);

    /// <summary>Gera um documento sintaticamente válido, para uso em testes e massa de dados.</summary>
    string Gerar(bool comMascara = false);
}

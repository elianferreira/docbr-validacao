namespace Api.Host.Modules.Documentos.Application;

/// <summary>Corpo da requisicao de validacao por numero digitado.</summary>
/// <param name="Numero">Numero do documento, com ou sem mascara.</param>
public sealed record EntradaNumero(string? Numero);

/// <summary>Um tipo de documento suportado, como aparece no catalogo.</summary>
/// <param name="Slug">Identificador estavel usado na URL (ex.: <c>titulo-eleitor</c>).</param>
/// <param name="Nome">Nome de exibicao do documento.</param>
/// <param name="Descricao">O que o validador cobre e o que ele nao prova.</param>
/// <param name="Tamanho">Quantidade de caracteres sem mascara.</param>
/// <param name="Experimental">Sem implementacao de referencia independente — tratar com reserva.</param>
/// <param name="Exemplo">Um numero valido, ja com mascara, que serve de placeholder do campo.</param>
public sealed record DocumentoResumo(
    string Slug,
    string Nome,
    string Descricao,
    int Tamanho,
    bool Experimental,
    string Exemplo);

/// <summary>Resultado da validacao de um numero contra um tipo especifico.</summary>
/// <param name="Tipo">Nome do documento validado.</param>
/// <param name="Experimental">Indica que o algoritmo nao tem referencia independente.</param>
/// <param name="Valido">Se passou em todas as verificacoes.</param>
/// <param name="Motivo">Motivo da recusa, ou <c>Nenhum</c>.</param>
/// <param name="Descricao">Mensagem legivel.</param>
/// <param name="Normalizado">Somente os digitos, quando foi possivel extrai-los.</param>
/// <param name="Formatado">Numero com mascara quando valido; o normalizado caso contrario.</param>
/// <param name="Subtipo">Nascimento, casamento ou obito — so a certidao preenche.</param>
public sealed record ResultadoValidacaoDto(
    string Tipo,
    bool Experimental,
    bool Valido,
    string Motivo,
    string Descricao,
    string Normalizado,
    string Formatado,
    string? Subtipo);

/// <summary>Um numero de exemplo gerado para o tipo pedido.</summary>
/// <param name="Numero">Numero sintaticamente valido, com mascara.</param>
public sealed record ExemploDto(string Numero);

/// <summary>Um tipo que aceitou o numero informado em <c>identificar</c>.</summary>
/// <param name="Slug">Identificador estavel do tipo.</param>
/// <param name="Nome">Nome de exibicao.</param>
/// <param name="Experimental">Indica que o algoritmo nao tem referencia independente.</param>
/// <param name="Formatado">O numero com a mascara deste tipo.</param>
public sealed record TipoIdentificado(string Slug, string Nome, bool Experimental, string Formatado);

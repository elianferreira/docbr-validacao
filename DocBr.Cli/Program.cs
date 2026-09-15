using DocBr.Validacao;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("Uso:");
    Console.WriteLine("  DocBr.Cli <tipo> <documento> [<documento> ...]   valida");
    Console.WriteLine("  DocBr.Cli <tipo> --gerar [n]                     gera n documentos válidos");
    Console.WriteLine("  DocBr.Cli --identificar <documento>              descobre em quais tipos o número passa");
    Console.WriteLine("  DocBr.Cli --tipos                                lista os tipos disponíveis");
    Console.WriteLine();
    Console.WriteLine("Tipos: " + string.Join(", ", CatalogoDocumentos.Todos.Select(validador => validador.Slug)));
    return 2;
}

if (args[0] is "--tipos")
{
    foreach (var validador in CatalogoDocumentos.Todos)
    {
        var marca = validador.Experimental ? " [experimental]" : string.Empty;
        Console.WriteLine($"{validador.Slug,-16} {validador.Nome}{marca}");
        Console.WriteLine($"{"",-16} {validador.Descricao}");
    }

    return 0;
}

if (args[0] is "--identificar")
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Informe o documento a identificar.");
        return 2;
    }

    var encontrados = CatalogoDocumentos.Identificar(args[1]);

    if (encontrados.Count == 0)
    {
        Console.WriteLine($"'{args[1]}' não passa na validação de nenhum tipo conhecido.");
        return 1;
    }

    Console.WriteLine($"'{args[1]}' passa na validação de:");

    foreach (var validador in encontrados)
    {
        var marca = validador.Experimental ? " (experimental)" : string.Empty;
        Console.WriteLine($"  {validador.Nome}{marca} — {validador.Formatar(args[1])}");
    }

    // Mais de um tipo aceitando o mesmo número é esperado: vários documentos têm 11 dígitos.
    if (encontrados.Count > 1)
        Console.WriteLine("Atenção: o número passa em mais de um tipo. Só o contexto diz qual é.");

    return 0;
}

var tipo = CatalogoDocumentos.PorSlug(args[0]);

if (tipo is null)
{
    Console.Error.WriteLine($"Tipo desconhecido: '{args[0]}'. Use --tipos para ver a lista.");
    return 2;
}

var restante = args.Skip(1).ToArray();

if (restante.Length > 0 && restante[0] is "--gerar" or "-g")
{
    var quantidade = restante.Length > 1 && int.TryParse(restante[1], out var n) ? n : 5;

    for (var i = 0; i < quantidade; i++)
        Console.WriteLine(tipo.Gerar(comMascara: true));

    return 0;
}

if (restante.Length == 0)
{
    Console.Error.WriteLine($"Informe ao menos um documento para validar, ou --gerar.");
    return 2;
}

var houveInvalido = false;

foreach (var entrada in restante)
{
    var resultado = tipo.Validar(entrada);
    var selo = resultado.Valido ? "VÁLIDO  " : "INVÁLIDO";

    Console.WriteLine($"{selo}  {entrada,-36}  {resultado.Descricao}");

    if (!resultado.Valido)
        houveInvalido = true;
}

return houveInvalido ? 1 : 0;

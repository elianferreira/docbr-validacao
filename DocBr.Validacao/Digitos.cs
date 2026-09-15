using System.Text;

namespace DocBr.Validacao;

/// <summary>Operações de texto comuns aos validadores.</summary>
internal static class Digitos
{
    /// <summary>Devolve apenas os algarismos da entrada.</summary>
    public static string Somente(string entrada)
    {
        var builder = new StringBuilder(entrada.Length);

        foreach (var caractere in entrada)
        {
            if (char.IsDigit(caractere))
                builder.Append(caractere);
        }

        return builder.ToString();
    }

    /// <summary>Devolve apenas algarismos e letras, em maiúsculas.</summary>
    public static string SomenteAlfanumericos(string entrada)
    {
        var builder = new StringBuilder(entrada.Length);

        foreach (var caractere in entrada)
        {
            if (char.IsLetterOrDigit(caractere))
                builder.Append(char.ToUpperInvariant(caractere));
        }

        return builder.ToString();
    }

    /// <summary>Indica que todos os caracteres da entrada são iguais (ex.: 11111111111).</summary>
    public static bool TodosIguais(string entrada)
    {
        if (entrada.Length == 0)
            return false;

        for (var i = 1; i < entrada.Length; i++)
        {
            if (entrada[i] != entrada[0])
                return false;
        }

        return true;
    }

    /// <summary>
    /// Verifica se a entrada contém apenas algarismos e os separadores informados.
    /// Devolve o primeiro caractere recusado, ou <c>null</c> quando está tudo certo.
    /// </summary>
    public static char? PrimeiroCaractereInvalido(string entrada, ReadOnlySpan<char> separadores, bool permiteLetras = false)
    {
        foreach (var caractere in entrada)
        {
            if (char.IsDigit(caractere))
                continue;

            if (permiteLetras && char.IsLetter(caractere))
                continue;

            if (separadores.Contains(caractere))
                continue;

            return caractere;
        }

        return null;
    }

    /// <summary>Valor numérico de um algarismo.</summary>
    public static int Valor(char algarismo) => algarismo - '0';

    /// <summary>Caractere de um valor de 0 a 9.</summary>
    public static char Algarismo(int valor) => (char)('0' + valor);
}

using System.Text.RegularExpressions;

namespace SauceDemo.Tests.Fixtures;

/// <summary>
/// Nomes de ficheiro portáveis entre sistemas. <c>Path.GetInvalidFileNameChars()</c> depende do SO: em Linux só exclui
/// '/' e '\0', e os nomes dos testes parametrizados têm aspas, que o NTFS e o upload de artefactos do GitHub rejeitam.
/// </summary>
public static partial class FileNames
{
    public static string ToSafe(string value) => UnsafeCharacters().Replace(value, "_");

    [GeneratedRegex("[^A-Za-z0-9._-]")]
    private static partial Regex UnsafeCharacters();
}

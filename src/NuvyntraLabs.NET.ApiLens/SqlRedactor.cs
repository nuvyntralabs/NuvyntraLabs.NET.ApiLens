using System.Text.RegularExpressions;

namespace NuvyntraLabs.NET.ApiLens;

public static partial class SqlRedactor
{
    public static string Redact(string commandText)
    {
        ArgumentNullException.ThrowIfNull(commandText);
        var withoutStrings = StringLiteral().Replace(commandText, "'?'");
        return NumericLiteral().Replace(withoutStrings, "?");
    }

    [GeneratedRegex(@"N?'(?:''|[^'])*'", RegexOptions.CultureInvariant)]
    private static partial Regex StringLiteral();

    [GeneratedRegex(@"(?<![\w@])\d+(?:\.\d+)?(?!\w)", RegexOptions.CultureInvariant)]
    private static partial Regex NumericLiteral();
}

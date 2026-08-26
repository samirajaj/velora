using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Velora.Infrastructure.Catalog;

internal static partial class SlugGenerator
{
    public static string Generate(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark) builder.Append(character);
        return DuplicateDashRegex().Replace(InvalidCharacterRegex().Replace(builder.ToString().ToLowerInvariant(), "-"), "-").Trim('-');
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex InvalidCharacterRegex();
    [GeneratedRegex("-{2,}")]
    private static partial Regex DuplicateDashRegex();
}

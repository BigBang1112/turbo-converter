using System.Text.RegularExpressions;

namespace TurboConverter;

internal static partial class RegexUtils
{
    [GeneratedRegex(@"Skins\\(.+)CE\\")]
    private static partial Regex SkinsCEMatch();

    public static string AdjustSkinReference(string filePath)
    {
        return SkinsCEMatch().Replace(filePath, "Skins\\$1\\");
    }
}

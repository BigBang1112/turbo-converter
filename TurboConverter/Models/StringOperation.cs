using System.Text.RegularExpressions;

namespace TurboConverter.Models;

public sealed class StringOperation
{
    public string? Match { get; set; }
    public string? ReplaceWith { get; set; }
    public string? Contains { get; set; }
    public string? Remove { get; set; }
    public string? Set { get; set; }
    public string? Prepend { get; set; }
    public string? Append { get; set; }
    public bool Safe { get; set; }

    public string Apply(string input = "", string? converterName = null)
    {
        var conditionIsMet = true;

        if (!string.IsNullOrEmpty(Contains))
        {
            conditionIsMet = input.Contains(Contains, StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrEmpty(Match))
        {
            conditionIsMet = Regex.IsMatch(input, Match);
        }

        if (!conditionIsMet)
        {
            if (Safe)
            {
                return input;
            }

            throw new Exception($"{input} does not meet the condition for converter '{converterName}'.");
        }

        if (!string.IsNullOrEmpty(ReplaceWith))
        {
            if (string.IsNullOrEmpty(Match))
            {
                throw new Exception($"Converter '{converterName}' does not have a match to replace.");
            }

            input = Regex.Replace(input, Match, ReplaceWith);
        }

        if (!string.IsNullOrEmpty(Remove))
        {
            input = input.Replace(Remove, "");
        }

        if (!string.IsNullOrEmpty(Set))
        {
            input = Set;
        }

        if (!string.IsNullOrEmpty(Prepend))
        {
            input = Prepend + input;
        }

        if (!string.IsNullOrEmpty(Append))
        {
            input += Append;
        }

        return input;
    }
}

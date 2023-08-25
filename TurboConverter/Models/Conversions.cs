namespace TurboConverter.Models;

public sealed class Conversions
{
    public Dictionary<string, BlockConversion?> Blocks { get; set; } = new();
}

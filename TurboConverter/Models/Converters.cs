namespace TurboConverter.Models;

public sealed class Converters
{
    public Dictionary<string, BlockConverter?> BlockConverters { get; set; } = new();
}

namespace TurboConverter.Models;

public sealed class SolidMappings
{
    public Dictionary<string, string> MP4 { get; set; } = [];
    public Dictionary<string, ItemModel[]?> TMT { get; set; } = [];
}

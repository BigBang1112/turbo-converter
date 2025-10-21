using GBX.NET;

namespace TurboConverter.Models;

public sealed class Conversions
{
    public Id? DefaultCollection { get; set; }
    public string? DefaultAuthor { get; set; }
    public int DecoBaseHeight { get; set; }
    public ItemModel? WarpItemModel { get; set; }
    public Dictionary<string, BlockConversion[]?>? Blocks { get; set; } = [];
}

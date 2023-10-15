namespace TurboConverter.Models;

public sealed class BlockVariantOf
{
    public string? Block { get; set; }
    public BlockConversion[] Variants { get; set; } = Array.Empty<BlockConversion>();
}

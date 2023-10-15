using GBX.NET;

namespace TurboConverter.Models;

public sealed class BlockConversion
{
    public string? Name { get; set; }
    public int? Variant { get; set; }
    public string? Converter { get; set; }
    public ItemModel? ItemModel { get; set; }
    public ItemModel[]? ItemModels { get; set; }
    public BlockConversion[]? SubVariants { get; set; }
    public Vec2? Size { get; set; }
    public BlockVariantOf? VariantOf { get; set; }
}

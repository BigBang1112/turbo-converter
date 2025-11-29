using GBX.NET;

namespace TurboConverter.Models;

public sealed class BlockConversion
{
    public string? Name { get; set; }
    public int DirOffset { get; set; }
    public int HeightOffset { get; set; }
    public int? Variant { get; set; }
    public string? Converter { get; set; }
    public string? ConverterAfter { get; set; }
    public ItemModel? ItemModel { get; set; }
    public ItemModel[]? ItemModels { get; set; }
    public BlockConversion[]? SubVariants { get; set; }
    public Vec2? Size { get; set; }
    public Dictionary<string, BlockConversion[]>? VariantOf { get; set; }
    public Dictionary<string, BlockConversion[]>? ModifierOf { get; set; }
    public string? DirectionOf { get; set; }
    public BlockConversion[]? ModifierFallback { get; set; }
    public BlockModel[]? AdditionalBlocks { get; set; }
    public string? Skin { get; set; }
    public int PreDirOffset { get; set; }

    public Dictionary<Direction, BlockConversion>? PerDirection { get; set; }
    public string? DirBlockReference { get; set; }
    public Dictionary<Direction, ItemModel[]>? DirBlockReferenceItemModels { get; set; }
}

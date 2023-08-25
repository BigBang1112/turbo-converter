namespace TurboConverter.Models;

public sealed class BlockConversion
{
    public string? Name { get; set; }
    public int? Variant { get; set; }
    public string? Converter { get; set; }
    public ItemModel? ItemModel { get; set; }
}

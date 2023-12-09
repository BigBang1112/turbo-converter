namespace TurboConverter.Models;

public sealed class BlockConverter
{
    public StringOperation? Name { get; set; }
    public ItemModel? ItemModel { get; set; }
    public StringOperation? Skin { get; set; }
    public string? ConverterAfter { get; set; }
}

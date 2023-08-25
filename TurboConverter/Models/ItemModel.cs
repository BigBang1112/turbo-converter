using GBX.NET;

namespace TurboConverter.Models;

public sealed class ItemModel
{
    public string? Id { get; set; }
    public Vec3? Pivot { get; set; }
    public Id? Collection { get; set; }
    public string? Author { get; set; }
}

using GBX.NET;
using GBX.NET.Engines.Game;
using TurboConverter.Models;

namespace TurboConverter.ConversionSystems;

internal sealed class WarpConversionSystem : IConversionSystem
{
    private readonly CGameCtnChallenge map;
    private readonly Conversions conversions;

    public WarpConversionSystem(CGameCtnChallenge map, Conversions conversions)
    {
        this.map = map;
        this.conversions = conversions;
    }

    public void Run()
    {
        if (conversions.WarpItemModel is not null)
        {
            var ident = new Ident(conversions.WarpItemModel.Id ?? throw new Exception("WarpItemModel ID is required"),
                conversions.WarpItemModel.Collection ?? conversions.DefaultCollection ?? map.Collection ?? throw new Exception("Map collection is null."),
                conversions.WarpItemModel.Author ?? conversions.DefaultAuthor ?? "");

            map.PlaceAnchoredObject(ident, new(), new());
        }
    }
}

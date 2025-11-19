using GBX.NET.Engines.Game;
using TmEssentials;

namespace TurboConverter.ConversionSystems;

internal sealed class MapUidConversionSystem : IConversionSystem
{
    private readonly CGameCtnChallenge map;

    public MapUidConversionSystem(CGameCtnChallenge map)
    {
        this.map = map;
    }

    public void Run()
    {
        // Generate unique map UID
        map.MapUid = $"{MapUtils.GenerateMapUid()[..10]}{map.MapUid.Substring(9, 10)}TMTURBO";
    }
}

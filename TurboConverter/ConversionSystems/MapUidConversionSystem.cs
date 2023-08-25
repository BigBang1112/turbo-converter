using System.Text;
using GBX.NET.Engines.Game;

namespace TurboConverter.ConversionSystems;

sealed class MapUidConversionSystem : IConversionSystem
{
    private readonly CGameCtnChallenge map;

    public MapUidConversionSystem(CGameCtnChallenge map)
    {
        this.map = map;
    }

    public void Run()
    {
        // Generate unique map UID
        map.MapUid = $"{Convert.ToBase64String(Encoding.ASCII.GetBytes(Guid.NewGuid().ToString()))[..10]}{map.MapUid.Substring(9, 10)}ENVIMIX";
    }
}

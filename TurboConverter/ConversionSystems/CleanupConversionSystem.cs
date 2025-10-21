using GBX.NET.Engines.Game;

namespace TurboConverter.ConversionSystems;

internal sealed class CleanupConversionSystem : IConversionSystem
{
    private readonly CGameCtnChallenge map;

    public CleanupConversionSystem(CGameCtnChallenge map)
    {
        this.map = map;
    }

    public void Run()
    {
        // Remove password chunk
        map.Chunks.Remove<CGameCtnChallenge.Chunk03043029>();

        // Remove lightmap chunk
        map.Chunks.Remove<CGameCtnChallenge.Chunk0304303D>();

        // Sometimes they might be placed on removed blocks, needs analysis to keep them
        map.Chunks.Remove<CGameCtnChallenge.Chunk0304303E>();

        // Some older maps have Trackmania\RaceCE MapType
        if (map.ChallengeParameters is not null)
        {
            map.ChallengeParameters.MapType = "Trackmania\\Race";
        }
    }
}

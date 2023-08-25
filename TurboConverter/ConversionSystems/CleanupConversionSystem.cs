using GBX.NET.Engines.Game;

namespace TurboConverter.ConversionSystems;

sealed class CleanupConversionSystem : IConversionSystem
{
    private readonly CGameCtnChallenge map;

    public CleanupConversionSystem(CGameCtnChallenge map)
    {
        this.map = map;
    }

    public void Run()
    {
        // Remove password chunk
        map.RemoveChunk<CGameCtnChallenge.Chunk03043029>();

        // Remove lightmap chunk
        map.RemoveChunk<CGameCtnChallenge.Chunk0304303D>();

        // Sometimes they might be placed on removed blocks, needs analysis to keep them
        map.RemoveChunk<CGameCtnChallenge.Chunk0304303E>();
    }
}

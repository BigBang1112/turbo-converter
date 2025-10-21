using GBX.NET.Engines.Game;

namespace TurboConverter.Extensions;

internal static class CGameCtnChallengeExtensions
{
    public static void RemoveBlockAt(this CGameCtnChallenge map, int blockIndex)
    {
        _ = map.Blocks ?? throw new Exception("Map blocks are null.");

        map.Blocks.RemoveAt(blockIndex);

        // If the following block is Unassigned1, also remove it
        if (map.Blocks.Count > blockIndex && map.Blocks[blockIndex].Name == "Unassigned1")
        {
            map.Blocks.RemoveAt(blockIndex);
        }
    }
}

using GBX.NET.Engines.Game;
using TurboConverter.Extensions;

namespace TurboConverter.ConversionSystems;

internal sealed class Unassigned1ConversionSystem : IConversionSystem
{
    private readonly CGameCtnChallenge map;

    public Unassigned1ConversionSystem(CGameCtnChallenge map)
    {
        this.map = map;
    }

    public void Run()
    {
        _ = map.Blocks ?? throw new Exception("Map blocks are null.");

        // Unassigned1 safety check 1
        // check if there are 2 Unassigned1 after each other
        for (var i = map.Blocks.Count - 1; i >= 0; i--)
        {
            var block = map.Blocks[i];

            if (block.Name == "Unassigned1")
            {
                if (i + 1 < map.Blocks.Count && map.Blocks[i + 1].Name == "Unassigned1")
                {
                    map.RemoveBlockAt(i);
                }
            }
        }

        // Unassigned1 safety check 2
        // add Unassigned1 at the end of Blocks if there is none
        /*
         * Broken on 001????
         * 
         * if (map.Blocks.Count > 0 && map.Blocks[^1].Name != "Unassigned1")
        {
            map.Blocks.Add(CGameCtnBlock.Unassigned1);
        }*/
    }
}

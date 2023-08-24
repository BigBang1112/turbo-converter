using GBX.NET.Engines.Game;
using GbxToolAPI;
using System.Linq;
using System.Text;
using TmEssentials;
using static GBX.NET.Engines.Hms.CHmsLightMapCache;

namespace TurboConverter;

[ToolName("Turbo Converter")]
[ToolDescription("Turbo Converter is a GBX.NET web tool.")]
[ToolAuthors("BigBang1112")]
[ToolAssets("TurboConverter")]
public class TurboConverterTool : ITool, IHasOutput<NodeFile<CGameCtnChallenge>>, IConfigurable<TurboConverterConfig>, IHasAssets
{
    private readonly CGameCtnChallenge map;

    public TurboConverterConfig Config { get; set; } = new();

    public Conversions? CanyonConversions { get; set; }

    public TurboConverterTool(CGameCtnChallenge map)
    {
        this.map = map;
    }

    public static string RemapAssetRoute(string route, bool isManiaPlanet)
    {
        return ""; // everything should stay the same
    }

    public async ValueTask LoadAssetsAsync()
    {
        CanyonConversions = await AssetsManager<TurboConverterTool>.GetFromYmlAsync<Conversions>("CanyonConversions.yml");
    }

    public NodeFile<CGameCtnChallenge> Produce()
    {
        _ = map.Blocks ?? throw new Exception("Map blocks are null.");

        // Remove password chunk
        map.RemoveChunk<CGameCtnChallenge.Chunk03043029>();

        // Remove lightmap chunk
        map.RemoveChunk<CGameCtnChallenge.Chunk0304303D>();

        // Generate unique map UID
        map.MapUid = $"{Convert.ToBase64String(Encoding.ASCII.GetBytes(Guid.NewGuid().ToString()))[..10]}{map.MapUid.Substring(9, 10)}ENVIMIX";

        // Backwards loop to allow for block removal
        for (var i = map.Blocks.Count - 1; i >= 0; i--)
        {
            var block = map.Blocks[i];

            // Boosts performance
            if (block.Name == "Unassigned1")
            {
                continue;
            }

            if (CanyonConversions?.Blocks.TryGetValue(block.Name, out var conversion) == true)
            {
                // Always remove the block
                map.Blocks.RemoveAt(i);

                // If the following block is Unassigned1, also remove it
                if (map.Blocks.Count > i && map.Blocks[i].Name == "Unassigned1")
                {
                    map.Blocks.RemoveAt(i);
                }

                continue;
            }

        }

        // configurable
        // map.AnchoredObjects?.Clear();

        var pureFileName = $"{TextFormatter.Deformat(map.MapName)}.Map.Gbx";
        var validFileName = string.Join("_", pureFileName.Split(Path.GetInvalidFileNameChars()));

        return new(map, $"Maps/TurboConverter/{validFileName}", IsManiaPlanet: true);
    }
}
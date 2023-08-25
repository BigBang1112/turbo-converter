using GBX.NET.Engines.Game;
using GbxToolAPI;
using System.Text;
using System.Text.RegularExpressions;
using TmEssentials;
using TurboConverter.Models;

namespace TurboConverter;

[ToolName("Turbo Converter")]
[ToolDescription("Turbo Converter is a GBX.NET web tool.")]
[ToolAuthors("BigBang1112")]
[ToolAssets("TurboConverter")]
public class TurboConverterTool : ITool, IHasOutput<NodeFile<CGameCtnChallenge>>, IConfigurable<TurboConverterConfig>, IHasAssets
{
    private readonly CGameCtnChallenge map;

    public TurboConverterConfig Config { get; set; } = new();

    public Conversions CanyonConversions { get; set; } = new();
    public Converters CanyonConverters { get; set; } = new();

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
        CanyonConverters = await AssetsManager<TurboConverterTool>.GetFromYmlAsync<Converters>("CanyonConverters.yml");
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

            if (CanyonConversions.Blocks.TryGetValue(block.Name, out var conversion))
            {
                ApplyConversion(block, i, conversion);
            }
        }

        // check if there are 2 Unassigned1 after each other
        for (var i = map.Blocks.Count - 1; i >= 0; i--)
        {
            var block = map.Blocks[i];

            if (block.Name == "Unassigned1")
            {
                if (i + 1 < map.Blocks.Count && map.Blocks[i + 1].Name == "Unassigned1")
                {
                    RemoveBlockAt(i);
                }
            }
        }

        // configurable
        // map.AnchoredObjects?.Clear();

        var pureFileName = $"{TextFormatter.Deformat(map.MapName)}.Map.Gbx";
        var validFileName = string.Join("_", pureFileName.Split(Path.GetInvalidFileNameChars()));

        return new(map, $"Maps/TurboConverter/{validFileName}", IsManiaPlanet: true);
    }

    private bool ApplyConversion(CGameCtnBlock block, int blockIndex, BlockConversion? conversion)
    {
        if (conversion is null)
        {
            RemoveBlockAt(blockIndex);
            return false;
        }

        if (!string.IsNullOrEmpty(conversion.Converter))
        {
            var converter = CanyonConverters.BlockConverters[conversion.Converter];
            
            if (converter is not null && converter.Name is not null)
            {
                var conditionIsMet = false;

                if (!string.IsNullOrEmpty(converter.Name.Contains))
                {
                    conditionIsMet = block.Name.Contains(converter.Name.Contains);
                }

                if (!string.IsNullOrEmpty(converter.Name.Match))
                {
                    conditionIsMet = Regex.IsMatch(block.Name, converter.Name.Match);
                }

                if (!conditionIsMet)
                {
                    throw new Exception($"Block {block.Name} does not meet the condition for converter {conversion.Converter}.");
                }

                if (!string.IsNullOrEmpty(converter.Name.ReplaceWith))
                {
                    if (string.IsNullOrEmpty(converter.Name.Match))
                    {
                        throw new Exception($"Converter {converter.Name} does not have a match to replace.");
                    }

                    block.Name = Regex.Replace(block.Name, converter.Name.Match, converter.Name.ReplaceWith);
                }

                if (!string.IsNullOrEmpty(converter.Name.Remove))
                {
                    block.Name = block.Name.Replace(converter.Name.Remove, "");
                }

                return true;
            }
        }

        RemoveBlockAt(blockIndex);
        return false;
    }

    private void RemoveBlockAt(int blockIndex)
    {
        map.Blocks!.RemoveAt(blockIndex);

        // If the following block is Unassigned1, also remove it
        if (map.Blocks.Count > blockIndex && map.Blocks[blockIndex].Name == "Unassigned1")
        {
            map.Blocks.RemoveAt(blockIndex);
        }
    }
}
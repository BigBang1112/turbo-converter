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

        // Sometimes they might be placed on removed blocks, needs analysis to keep them
        map.RemoveChunk<CGameCtnChallenge.Chunk0304303E>();

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

            if (CanyonConversions.Blocks.TryGetValue(block.Name, out var converter))
            {
                ApplyConverter(block, i, converter);
            }
        }

        // Unassigned1 safety check 1
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

        // Unassigned1 safety check 2
        // add Unassigned1 at the end of Blocks if there is none
        if (map.Blocks.Count > 0 && map.Blocks[^1].Name != "Unassigned1")
        {
            map.Blocks.Add(CGameCtnBlock.Unassigned1);
        }

        // configurable
        // map.AnchoredObjects?.Clear();

        var pureFileName = $"{TextFormatter.Deformat(map.MapName)}.Map.Gbx";
        var validFileName = string.Join("_", pureFileName.Split(Path.GetInvalidFileNameChars()));

        return new(map, $"Maps/TurboConverter/{validFileName}", IsManiaPlanet: true);
    }

    private bool ApplyConverter(CGameCtnBlock block, int blockIndex, BlockConverter? converter)
    {
        if (converter is null)
        {
            RemoveBlockAt(blockIndex);
            return false;
        }

        if (converter is not null)
        {
            if (!string.IsNullOrEmpty(converter.Converter))
            {
                converter = CanyonConverters.BlockConverters[converter.Converter];

                return ApplyConverter(block, blockIndex, converter);
            }

            if (converter.Name is not null)
            {
                ApplyStringOperation(block, converter.Name, converter.Converter);
                return true;
            }
        }

        RemoveBlockAt(blockIndex);
        return false;
    }

    private static void ApplyStringOperation(CGameCtnBlock block, StringOperation operation, string? converterName)
    {
        var conditionIsMet = true;

        if (!string.IsNullOrEmpty(operation.Contains))
        {
            conditionIsMet = block.Name.Contains(operation.Contains);
        }

        if (!string.IsNullOrEmpty(operation.Match))
        {
            conditionIsMet = Regex.IsMatch(block.Name, operation.Match);
        }

        if (!conditionIsMet)
        {
            throw new Exception($"Block {block.Name} does not meet the condition for converter {converterName ?? "[default]"}.");
        }

        if (!string.IsNullOrEmpty(operation.ReplaceWith))
        {
            if (string.IsNullOrEmpty(operation.Match))
            {
                throw new Exception($"Converter {converterName ?? "[default]"} does not have a match to replace.");
            }

            block.Name = Regex.Replace(block.Name, operation.Match, operation.ReplaceWith);
        }

        if (!string.IsNullOrEmpty(operation.Remove))
        {
            block.Name = block.Name.Replace(operation.Remove, "");
        }

        if (!string.IsNullOrEmpty(operation.Set))
        {
            block.Name = operation.Set;
        }
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
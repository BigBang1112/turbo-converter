using GBX.NET.Engines.Game;
using GbxToolAPI;
using TmEssentials;
using TurboConverter.ConversionSystems;
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
    public Conversions StadiumConversions { get; set; } = new();
    public Conversions ValleyConversions { get; set; } = new();
    public Conversions LagoonConversions { get; set; } = new();

    public Converters Converters { get; set; } = new();

    public TurboConverterTool(CGameCtnChallenge map)
    {
        this.map = map;
    }

    public static string RemapAssetRoute(string route, bool isManiaPlanet)
    {
        return route; // everything should stay the same
    }

    public async ValueTask LoadAssetsAsync()
    {
        CanyonConversions = await AssetsManager<TurboConverterTool>.GetFromYmlAsync<Conversions>("CanyonConversions.yml");
        StadiumConversions = await AssetsManager<TurboConverterTool>.GetFromYmlAsync<Conversions>("StadiumConversions.yml");
        ValleyConversions = await AssetsManager<TurboConverterTool>.GetFromYmlAsync<Conversions>("ValleyConversions.yml");
        LagoonConversions = await AssetsManager<TurboConverterTool>.GetFromYmlAsync<Conversions>("LagoonConversions.yml");
        Converters = await AssetsManager<TurboConverterTool>.GetFromYmlAsync<Converters>("Converters.yml");
    }

    public NodeFile<CGameCtnChallenge> Produce()
    {
        var conversions = (string)map.Collection switch
        {
            "Canyon" => CanyonConversions,
            "Stadium" => StadiumConversions,
            "Valley" => ValleyConversions,
            "Lagoon" => LagoonConversions,
            _ => throw new Exception($"Collection {map.Collection} is not supported."),
        };

        var originalMapInfo = new OriginalMapInfo(map);

        new MapUidConversionSystem(map).Run();

        var blockConversionSystem = new BlockConversionSystem(map, conversions, Converters);
        blockConversionSystem.Run();

        new Unassigned1ConversionSystem(map).Run();
        new WarpConversionSystem(map, conversions).Run();
        new CleanupConversionSystem(map).Run();
        new MetadataConversionSystem(map, originalMapInfo, blockConversionSystem.ConvertedBlocks).Run();

        // configurable
        // map.AnchoredObjects?.Clear();

        var pureFileName = $"{TextFormatter.Deformat(map.MapName)}.Map.Gbx";
        var validFileName = string.Join("_", pureFileName.Split(Path.GetInvalidFileNameChars()));

        return new(map, $"Maps/TurboConverter/{validFileName}", IsManiaPlanet: true);
    }
}
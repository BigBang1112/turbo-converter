using GBX.NET.Engines.Game;
using GbxToolAPI;
using System.Text;
using System.Text.RegularExpressions;
using TmEssentials;
using TurboConverter.ConversionSystems;
using TurboConverter.Extensions;
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

    public Converters CanyonConverters { get; set; } = new();
    public Converters StadiumConverters { get; set; } = new();
    public Converters ValleyConverters { get; set; } = new();
    public Converters LagoonConverters { get; set; } = new();

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
        StadiumConversions = await AssetsManager<TurboConverterTool>.GetFromYmlAsync<Conversions>("StadiumConversions.yml");
        ValleyConversions = await AssetsManager<TurboConverterTool>.GetFromYmlAsync<Conversions>("ValleyConversions.yml");
        LagoonConversions = await AssetsManager<TurboConverterTool>.GetFromYmlAsync<Conversions>("LagoonConversions.yml");
        CanyonConverters = await AssetsManager<TurboConverterTool>.GetFromYmlAsync<Converters>("CanyonConverters.yml");
        StadiumConverters = await AssetsManager<TurboConverterTool>.GetFromYmlAsync<Converters>("StadiumConverters.yml");
        ValleyConverters = await AssetsManager<TurboConverterTool>.GetFromYmlAsync<Converters>("ValleyConverters.yml");
        LagoonConverters = await AssetsManager<TurboConverterTool>.GetFromYmlAsync<Converters>("LagoonConverters.yml");
    }

    public NodeFile<CGameCtnChallenge> Produce()
    {
        Conversions conversions;
        Converters converters;

        switch (map.Collection)
        {
            case "Canyon":
                conversions = CanyonConversions;
                converters = CanyonConverters;
                break;
            case "Stadium":
                conversions = StadiumConversions;
                converters = StadiumConverters;
                break;
            case "Valley":
                conversions = ValleyConversions;
                converters = ValleyConverters;
                break;
            case "Lagoon":
                conversions = LagoonConversions;
                converters = LagoonConverters;
                break;
            default:
                throw new Exception($"Collection {map.Collection} is not supported.");
        }

        new MapUidConversionSystem(map).Run();
        new BlockConversionSystem(map, conversions, converters).Run();
        new Unassigned1ConversionSystem(map).Run();
        new CleanupConversionSystem(map).Run();

        // configurable
        // map.AnchoredObjects?.Clear();

        var pureFileName = $"{TextFormatter.Deformat(map.MapName)}.Map.Gbx";
        var validFileName = string.Join("_", pureFileName.Split(Path.GetInvalidFileNameChars()));

        return new(map, $"Maps/TurboConverter/{validFileName}", IsManiaPlanet: true);
    }
}
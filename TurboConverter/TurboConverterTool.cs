using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Tool;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;
using TmEssentials;
using TurboConverter.ConversionSystems;
using TurboConverter.Models;

namespace TurboConverter;

public class TurboConverterTool : ITool,
    IMutative<Gbx<CGameCtnChallenge>>,
    IConfigurable<TurboConverterConfig>
{
    private readonly Gbx<CGameCtnChallenge> gbxMap;
    private readonly CGameCtnChallenge map;
    private readonly IComplexConfig complexConfig;
    private readonly ILogger logger;

    private static readonly ImmutableHashSet<string> supportedCollections = ImmutableHashSet.Create(
        "Canyon",
        "Stadium",
        "Valley",
        "Lagoon"
    );

    public TurboConverterConfig Config { get; } = new();

    public TurboConverterTool(Gbx<CGameCtnChallenge> gbxMap, IComplexConfig complexConfig, ILogger logger)
    {
        this.gbxMap = gbxMap;
        this.complexConfig = complexConfig;
        this.logger = logger;

        map = gbxMap.Node;
    }

    public Gbx<CGameCtnChallenge>? Mutate()
    {
        if (!supportedCollections.Contains(map.Collection?.ToString() ?? ""))
        {
            throw new InvalidOperationException($"Map collection '{map.Collection}' is not supported for conversion.");
        }

        var conversions = complexConfig.Get<Conversions>($"{map.Collection}Conversions", cache: true);
        var converters = complexConfig.Get<Converters>("Converters", cache: true);

        var originalMapInfo = new OriginalMapInfo(map);

        new MapUidConversionSystem(map).Run();

        var blockConversionSystem = new BlockConversionSystem(map, conversions, converters);
        blockConversionSystem.Run();

        new Unassigned1ConversionSystem(map).Run();
        new SkinFixupConversionSystem(map).Run();
        new WarpConversionSystem(map, conversions).Run();
        new CleanupConversionSystem(map).Run();
        new MetadataConversionSystem(map, originalMapInfo, blockConversionSystem.ConvertedBlocks).Run();

        // configurable
        // map.AnchoredObjects?.Clear();

        var fileName = Path.GetFileName(gbxMap.FilePath)
            ?? Path.GetInvalidFileNameChars().Aggregate(TextFormatter.Deformat(map.MapName), (current, c) => current.Replace(c, '_')); 

        gbxMap.FilePath = Path.Combine("Maps", "GbxTools", "TurboConverter", fileName);

        return gbxMap;
    }
}

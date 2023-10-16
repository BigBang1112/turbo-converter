using GBX.NET.Engines.Game;
using GBX.NET.Engines.Script;
using TurboConverter.Models;
using static GBX.NET.Engines.Script.CScriptTraitsMetadata;

namespace TurboConverter.ConversionSystems;

sealed class MetadataConversionSystem : IConversionSystem
{
    private readonly CGameCtnChallenge map;
    private readonly OriginalMapInfo originalMapInfo;
    private readonly IList<CScriptTraitsMetadata.ScriptStructTrait> convertedBlocks;

    public MetadataConversionSystem(CGameCtnChallenge map, OriginalMapInfo originalMapInfo, IList<CScriptTraitsMetadata.ScriptStructTrait> convertedBlocks)
    {
        this.map = map;
        this.originalMapInfo = originalMapInfo;
        this.convertedBlocks = convertedBlocks;
    }

    public void Run()
    {
        _ = map.ScriptMetadata ?? throw new Exception("ScriptMetadata is null");

        map.ScriptMetadata.CreateChunk<CScriptTraitsMetadata.Chunk11002000>().Version = 5;
        
        var assemblyName = typeof(TurboConverterTool).Assembly.GetName();

        map.ScriptMetadata.Declare("MadeWithTurboConverter", true);
        map.ScriptMetadata.Declare("TC_Reverse", false);

        if (assemblyName.Version is not null)
        {
            map.ScriptMetadata.Declare("TC_Version", assemblyName.Version.ToString());
        }

        map.ScriptMetadata.Declare("TC_OriginalAuthorLogin", originalMapInfo.AuthorLogin);

        if (originalMapInfo.AuthorNickname is not null)
        {
            map.ScriptMetadata.Declare("TC_OriginalAuthorNickname", originalMapInfo.AuthorNickname);
        }
        map.ScriptMetadata.Declare("TC_OriginalMapUid", originalMapInfo.MapUid);

        if (convertedBlocks.Count > 0)
        {
            map.ScriptMetadata.Traits["TC_ConvertedBlocks"] = new ScriptArrayTrait(new ScriptArrayType(new ScriptType(EScriptType.Void), convertedBlocks[0].Type), convertedBlocks.Select((Func<ScriptStructTrait, ScriptTrait>)((ScriptStructTrait x) => x)).ToList());
        }
    }
}

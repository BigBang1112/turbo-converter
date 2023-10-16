using GBX.NET.Engines.Game;
using TurboConverter.Models;

namespace TurboConverter.ConversionSystems;

sealed class MetadataConversionSystem : IConversionSystem
{
    private readonly CGameCtnChallenge map;
    private readonly OriginalMapInfo originalMapInfo;

    public MetadataConversionSystem(CGameCtnChallenge map, OriginalMapInfo originalMapInfo)
    {
        this.map = map;
        this.originalMapInfo = originalMapInfo;
    }

    public void Run()
    {
        _ = map.ScriptMetadata ?? throw new Exception("ScriptMetadata is null");

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
    }
}

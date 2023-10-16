using GBX.NET.Engines.Game;
using GBX.NET.Engines.Script;

namespace TurboConverter.Models;

sealed class OriginalMapInfo
{
    public string AuthorLogin { get; }
    public string? AuthorNickname { get; }
    public string MapUid { get; }

    public OriginalMapInfo(CGameCtnChallenge map)
    {
        AuthorLogin = map.AuthorLogin;
        AuthorNickname = map.AuthorNickname;
        MapUid = map.MapUid;
    }
}

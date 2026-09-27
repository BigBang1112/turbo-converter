using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using TurboConverter.Models;

namespace TurboConverter.ConversionSystems;

internal sealed class ItemConversionSystem : IConversionSystem
{
    private readonly CGameCtnChallenge map;

    public ItemConversionSystem(CGameCtnChallenge map)
    {
        this.map = map;
    }

    public void Run()
    {
        if (map.EmbeddedZipData is null or { Length: 0 })
        {
            return;
        }

        map.UpdateEmbeddedZipData(zip =>
        {
            foreach (var entry in zip.Entries)
            {
                using var stream = entry.Open();

                var gbx = Gbx.Parse(stream);

                if (gbx.Node is CPlugSolid2Model solid2)
                {
                    solid2.MaterialsFolderName = ReplaceCE(solid2.MaterialsFolderName);
                }
                else if (gbx.Node is CGameItemModel item)
                {
                    if (item.VisModelCustom is not CGameObjectVisModel { SolidRef: not null } visModel)
                    {
                        continue;
                    }

                    visModel.SolidRef = ReplaceCE(visModel.SolidRef);
                    visModel.MeshShaded = null; // important for the item to display

                    if (item.PhyModelCustom is CGameObjectPhyModel phyModel)
                    {
                        var chunk001 = phyModel.GetChunk<CGameObjectPhyModel.Chunk2E006001>();
                        chunk001?.U14 = ReplaceCE(chunk001?.U14);
                    }
                }
                else
                {
                    continue;
                }

                using var updatedGbx = new MemoryStream();
                gbx.Save(updatedGbx);
                updatedGbx.Position = 0;

                stream.SetLength(0);
                updatedGbx.CopyTo(stream);
            }
        });
    }

    private static string ReplaceCE(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "";
        }

        return string.Join("\\",
            path.Split('\\')
                .Select(part =>
                    part.EndsWith("CE", StringComparison.OrdinalIgnoreCase)
                        ? part[..^2]
                        : part));
    }
}

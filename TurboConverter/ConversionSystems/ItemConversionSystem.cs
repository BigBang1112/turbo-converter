using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.GameData;
using GBX.NET.Engines.Plug;
using TurboConverter.Models;

namespace TurboConverter.ConversionSystems;

internal sealed class ItemConversionSystem : IConversionSystem
{
    private readonly CGameCtnChallenge map;
    private readonly Dictionary<string, string> mp4;
    private readonly Dictionary<string, ItemModel[]?> tmt;

    public ItemConversionSystem(CGameCtnChallenge map, SolidMappings solidMappings)
    {
        this.map = map;
        mp4 = solidMappings.MP4.ToDictionary(StringComparer.OrdinalIgnoreCase);
        tmt = solidMappings.TMT.ToDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public void Run()
    {
        if (map.EmbeddedZipData is null or { Length: 0 })
        {
            return;
        }

        var anchoredObjectsById = map.GetAnchoredObjects()
            .ToLookup(x => NormalizeItemId(x.ItemModel.Id), StringComparer.OrdinalIgnoreCase);
        var replacedAnchoredObjects = new Dictionary<CGameCtnAnchoredObject, CGameCtnAnchoredObject>();

        map.UpdateEmbeddedZipData(zip =>
        {
            foreach (var entry in zip.Entries.ToArray())
            {
                if (!entry.FullName.EndsWith(".Gbx", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Gbx gbx;
                using (var stream = entry.Open())
                {
                    gbx = Gbx.Parse(stream);
                }

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

                    var solidRefNoPrefix = visModel.SolidRef.Replace('/', '\\').TrimStart('\\');

                    if (tmt.TryGetValue(solidRefNoPrefix, out var itemModels))
                    {
                        if (itemModels is { Length: > 0 })
                        {
                            foreach (var original in anchoredObjectsById[NormalizeItemId(entry.FullName)])
                            {
                                var replacements = itemModels.Select(itemModel => CopyAnchoredObject(original, itemModel)).ToArray();
                                replacedAnchoredObjects.Add(original, replacements[0]);
                                map.RemoveAnchoredObject(original);
                            }

                            entry.Delete();
                        }

                        continue;
                    }

                    var replace = ReplaceCE;

                    if (mp4.TryGetValue(solidRefNoPrefix, out var replacement))
                    {
                        replace = _ => replacement;
                    }

                    visModel.SolidRef = replace(visModel.SolidRef);
                    visModel.MeshShaded = null; // important for the item to display

                    if (item.PhyModelCustom is CGameObjectPhyModel phyModel)
                    {
                        var chunk001 = phyModel.GetChunk<CGameObjectPhyModel.Chunk2E006001>();
                        chunk001?.U14 = replace(chunk001?.U14);
                    }
                }
                else
                {
                    continue;
                }

                using var updatedGbx = new MemoryStream();
                gbx.Save(updatedGbx);
                updatedGbx.Position = 0;

                using var updatedStream = entry.Open();
                updatedStream.SetLength(0);
                updatedGbx.CopyTo(updatedStream);
            }
        });

        foreach (var anchoredObject in map.GetAnchoredObjects())
        {
            if (anchoredObject.SnappedOnItem is not null && replacedAnchoredObjects.TryGetValue(anchoredObject.SnappedOnItem, out var snappedOnItem))
            {
                anchoredObject.SnappedOnItem = snappedOnItem;
            }

            if (anchoredObject.PlacedOnItem is not null && replacedAnchoredObjects.TryGetValue(anchoredObject.PlacedOnItem, out var placedOnItem))
            {
                anchoredObject.PlacedOnItem = placedOnItem;
            }
        }
    }

    private CGameCtnAnchoredObject CopyAnchoredObject(CGameCtnAnchoredObject original, ItemModel itemModel)
    {
        var collection = map.Collection ?? original.ItemModel.Collection;
        var id = (itemModel.Id ?? throw new InvalidOperationException("TMT item model ID is missing."))
            .Replace("{1}", collection.ToString());
        var ident = new Ident(id, itemModel.Collection ?? collection, itemModel.Author ?? "bigbang1112");
        var replacement = map.PlaceAnchoredObject(ident, original.AbsolutePositionInMap, original.YawPitchRoll, original.PivotPosition);

        replacement.Scale = original.Scale;
        replacement.BlockUnitCoord = original.BlockUnitCoord;
        replacement.AnchorTreeId = original.AnchorTreeId;
        replacement.WaypointSpecialProperty = original.WaypointSpecialProperty;
        replacement.Flags = original.Flags;
        replacement.PackDesc = original.PackDesc;
        replacement.ForegroundPackDesc = original.ForegroundPackDesc;
        replacement.SnappedOnBlock = original.SnappedOnBlock;
        replacement.SnappedOnItem = original.SnappedOnItem;
        replacement.PlacedOnItem = original.PlacedOnItem;
        replacement.SnappedOnGroup = original.SnappedOnGroup;
        replacement.Color = original.Color;
        replacement.AnimPhaseOffset = original.AnimPhaseOffset;
        replacement.LightmapQuality = original.LightmapQuality;
        replacement.MacroblockReference = original.MacroblockReference;

        // Keep the original chunk versions and any extra serialized placement data.
        if (original.Chunks.Count > 0)
        {
            replacement.Chunks.Clear();
            foreach (var chunk in original.Chunks)
            {
                replacement.Chunks.Add(chunk);
            }
        }

        return replacement;
    }

    private static string NormalizeItemId(string id)
    {
        id = id.Replace('/', '\\').TrimStart('\\');
        return id.StartsWith("Items\\", StringComparison.OrdinalIgnoreCase) ? id[6..] : id;
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

using GBX.NET.Engines.Game;
using Microsoft.VisualBasic;
using System;

namespace TurboConverter.ConversionSystems;

sealed class SkinFixupConversionSystem : IConversionSystem
{
    private readonly CGameCtnChallenge map;
    private readonly HashSet<CGameCtnBlockSkin> modifiedSkins = new();

    public SkinFixupConversionSystem(CGameCtnChallenge map)
    {
        this.map = map;
    }

    public void Run()
    {
        foreach (var block in map.GetBlocks())
        {
            if (block.Skin is null)
            {
                continue;
            }

            if (modifiedSkins.Contains(block.Skin))
            {
                continue;
            }

            if (!string.IsNullOrEmpty(block.Skin.PackDesc.FilePath))
            {
                block.Skin.PackDesc = block.Skin.PackDesc with { FilePath = RegexUtils.AdjustSkinReference(block.Skin.PackDesc.FilePath) };
            }

            if (block.Skin.ParentPackDesc is not null && !string.IsNullOrEmpty(block.Skin.ParentPackDesc.FilePath))
            {
                block.Skin.ParentPackDesc = block.Skin.ParentPackDesc with { FilePath = RegexUtils.AdjustSkinReference(block.Skin.ParentPackDesc.FilePath) };
            }

            modifiedSkins.Add(block.Skin);
        }
    }
}

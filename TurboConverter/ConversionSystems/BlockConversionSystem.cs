using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Script;
using TurboConverter.Extensions;
using TurboConverter.Models;

namespace TurboConverter.ConversionSystems;

internal sealed class BlockConversionSystem : IConversionSystem
{
    private readonly CGameCtnChallenge map;
    private readonly Conversions conversions;
    private readonly Converters converters;
    private readonly Int3 blockSize;
    private readonly ILookup<Int3, CGameCtnBlock> blocksByCoord;
    private readonly HashSet<CGameCtnBlockSkin> modifiedSkins = [];
    
    public IList<CScriptTraitsMetadata.ScriptStructTrait> ConvertedBlocks { get; } = [];

    public BlockConversionSystem(CGameCtnChallenge map, Conversions conversions, Converters converters)
    {
        this.map = map;
        this.conversions = conversions;
        this.converters = converters;

        blockSize = map.Collection?.GetBlockSize() ?? throw new Exception("Map collection is null.");

        _ = map.Blocks ?? throw new Exception("Map blocks are null.");
        blocksByCoord = map.Blocks.ToLookup(x => x.Coord);
    }

    public void Run()
    {
        _ = map.Blocks ?? throw new Exception("Map blocks are null.");

        // Backwards loop to allow for block removal
        for (var i = map.Blocks.Count - 1; i >= 0; i--)
        {
            ApplyBlockConversion(map.Blocks[i], i);
        }

        foreach (var block in map.GetBakedBlocks())
        {
            if (conversions.Tanks?.Contains(block.Name) == true)
            {
                ApplyBlockConversion(block, -1, new() { ItemModel = new() { Id = "CE\\Tank\\{1}_{0}.Item.Gbx" } }, out _);
            }
        }
    }

    private void ApplyBlockConversion(CGameCtnBlock block, int blockIndex)
    {
        var previousBlockName = block.Name;

        if (conversions.Blocks?.TryGetValue(block.Name, out var conversion) == true)
        {
            var convertedBlockStructBuilder = CScriptTraitsMetadata.CreateStruct("SConvertedBlock")
                .WithText("Name", block.Name)
                .WithInt3("Coord", block.Coord);

            if (conversion is null)
            {
                map.RemoveBlockAt(blockIndex);
                return;
            }

            var removeBlock = true;

            if (conversion.Length > block.Variant)
            {
                ApplyBlockConversion(block, blockIndex, conversion[block.Variant], out removeBlock);
            }
            else
            {
                Console.WriteLine($"Block {block.Name} has variant {block.Variant} but only {conversion.Length} variants are defined. Block will be removed.");
            }

            if (removeBlock)
            {
                map.RemoveBlockAt(blockIndex);
            }

            var convertedBlockStruct = convertedBlockStructBuilder.Build();

            ConvertedBlocks.Add(convertedBlockStruct);
        }
    }

    private void ApplyBlockConversion(CGameCtnBlock block, int blockIndex, BlockConversion? conversion, out bool removeBlock)
    {
        removeBlock = true;

        if (conversion is null)
        {
            return;
        }

        block.Direction = (Direction)(((int)block.Direction + conversion.PreDirOffset) % 4);

        if (conversion.DirectionOf is not null)
        {
            var blockForDirection = blocksByCoord[block.Coord].FirstOrDefault(x => conversion.DirectionOf == x.Name);

            if (blockForDirection is not null)
            {
                block.Direction = (Direction)(((int)blockForDirection.Direction + 2) % 4);
            }
        }

        if (conversion.PerDirection is not null)
        {
            if (conversion.PerDirection.TryGetValue(block.Direction, out var directionConversion))
            {
                ApplyBlockConversion(block, blockIndex, directionConversion, out removeBlock);
            }
        }

        if (conversion.DirBlockReference is not null)
        {
            var blockForDirection = blocksByCoord[block.Coord].FirstOrDefault(x => conversion.DirBlockReference == x.Name);

            if (blockForDirection is not null && conversion.DirBlockReferenceItemModels?.TryGetValue(blockForDirection.Direction, out var itemModels) == true)
            {
                foreach (var itemModel in itemModels)
                {
                    PlaceAnchoredObject(block, itemModel, conversion.Size);
                }
            }
        }

        if (conversion.ItemModel is not null)
        {
            PlaceAnchoredObject(block, conversion.ItemModel, conversion.Size);
        }

        if (conversion.ItemModels is not null)
        {
            foreach (var itemModel in conversion.ItemModels)
            {
                PlaceAnchoredObject(block, itemModel, conversion.Size);
            }
        }

        if (conversion.VariantOf is not null)
        {
            var blockForVariant = blocksByCoord[block.Coord].FirstOrDefault(x => conversion.VariantOf.ContainsKey(x.Name));

            if (blockForVariant is not null)
            {
                ApplyBlockConversion(block, blockIndex, conversion.VariantOf[blockForVariant.Name][blockForVariant.Variant], out removeBlock);
                return;
            }
        }

        if (conversion.ModifierOf is not null)
        {
            var modifierApplied = false;

            // for loop from Coord Y to zero
            for (var y = block.Coord.Y; y >= 0; y--)
            {
                var blocksAtCoord = blocksByCoord[new Int3(block.Coord.X, y, block.Coord.Z)];
                var blockForModifier = blocksAtCoord.FirstOrDefault(x => conversion.ModifierOf.ContainsKey(x.Name));
                if (blockForModifier is not null)
                {
                    ApplyBlockConversion(block, blockIndex, conversion.ModifierOf[blockForModifier.Name][block.Variant], out removeBlock);
                    modifierApplied = true;
                    break;
                }
            }

            if (!modifierApplied && conversion.ModifierFallback is not null)
            {
                ApplyBlockConversion(block, blockIndex, conversion.ModifierFallback[block.Variant], out removeBlock);
            }
        }

        if (!string.IsNullOrEmpty(conversion.Converter))
        {
            ApplyConverter(block, conversion.Converter, conversion);
            removeBlock = false;
        }

        if (!string.IsNullOrEmpty(conversion.Name))
        {
            block.Name = string.Format(conversion.Name, GetBlockStringArgs(block));
            removeBlock = false;
        }

        block.Direction = (Direction)(((int)block.Direction + conversion.DirOffset) % 4);
        block.Coord += (0, conversion.HeightOffset, 0);

        if (conversion.HeightOffset != 0)
        {
            removeBlock = false;
        }

        if (conversion.Variant.HasValue)
        {
            block.Variant = (byte)conversion.Variant.Value;
            removeBlock = false;
        }

        if (conversion.SubVariants?.Length > 0)
        {
            ApplyBlockConversion(block, blockIndex, conversion.SubVariants[block.SubVariant], out removeBlock);
        }

        if (conversion.AdditionalBlocks is not null)
        {
            foreach (var additionalBlock in conversion.AdditionalBlocks)
            {
                var blockName = additionalBlock.Name ?? conversion.Name;

                if (string.IsNullOrEmpty(blockName))
                {
                    continue;
                }

                map.PlaceBlock(blockName, block.Coord + (0, additionalBlock.OffsetY, 0), block.Direction, additionalBlock.IsGround ?? block.IsGround);
            }
        }

        if (!string.IsNullOrEmpty(conversion.ConverterAfter))
        {
            ApplyConverter(block, conversion.ConverterAfter, conversion);
            removeBlock = false;
        }

        if (!string.IsNullOrWhiteSpace(conversion.Skin))
        {
            if (block.Skin is null)
            {
                block.Skin = new CGameCtnBlockSkin();
                block.Skin.CreateChunk<CGameCtnBlockSkin.Chunk03059002>();
                block.Author = "Nadeo";
            }

            block.Skin.PackDesc = new()
            {
                FilePath = string.Format(conversion.Skin, GetBlockStringArgs(block))
            };

            removeBlock = false;
        }
    }

    private void ApplyConverter(CGameCtnBlock block, string converterName, BlockConversion conversion)
    {
        if (!converters.BlockConverters.TryGetValue(converterName, out var converter))
        {
            throw new Exception($"Converter {converterName} does not exist.");
        }

        if (converter is null)
        {
            // throw new Exception($"Converter {conversion.Converter} is not implemented.");
        }
        else
        {
            ApplyGenericConverter(block, converter, conversion);
        }
    }

    private void ApplyGenericConverter(CGameCtnBlock block, BlockConverter converter, BlockConversion conversion)
    {
        if (converter.ItemModel is not null)
        {
            PlaceAnchoredObject(block, converter.ItemModel, conversion.Size);
        }

        if (converter.ItemModels is not null)
        {
            foreach (var itemModel in converter.ItemModels)
            {
                PlaceAnchoredObject(block, itemModel, conversion.Size);
            }
        }

        if (converter.Name is not null)
        {
            block.Name = string.Format(converter.Name.Apply(block.Name, conversion.Converter), GetBlockStringArgs(block));
        }

        if (block.Skin is not null && converter.Skin is not null && !modifiedSkins.Contains(block.Skin))
        {
            if (!string.IsNullOrEmpty(block.Skin.PackDesc?.FilePath))
            {
                block.Skin.PackDesc = block.Skin.PackDesc with { FilePath = converter.Skin.Apply(block.Skin.PackDesc.FilePath, conversion.Converter) };
            }

            if (block.Skin.ParentPackDesc is not null && !string.IsNullOrEmpty(block.Skin.ParentPackDesc.FilePath))
            {
                block.Skin.ParentPackDesc = block.Skin.ParentPackDesc with { FilePath = converter.Skin.Apply(block.Skin.ParentPackDesc.FilePath, conversion.Converter) };
            }

            modifiedSkins.Add(block.Skin);
        }

        if (!string.IsNullOrEmpty(converter.ConverterAfter))
        {
            if (!converters.BlockConverters.TryGetValue(converter.ConverterAfter, out var converterAfter))
            {
                throw new Exception($"Converter {conversion.Converter} does not exist.");
            }

            if (converterAfter is null)
            {
                // throw new Exception($"Converter {conversion.Converter} is not implemented.");
            }
            else
            {
                ApplyGenericConverter(block, converterAfter, conversion);
            }
        }
    }

    private object[] GetBlockStringArgs(CGameCtnBlock block)
    {
        return [
            block.Name,
            map.Collection ?? throw new Exception("Map collection is null."),
            block.IsGround ? "Ground" : "Air",
            Path.GetFileNameWithoutExtension(block.Skin?.PackDesc?.FilePath) ?? "WELCOME_TM"
        ];
    }

    private void PlaceAnchoredObject(CGameCtnBlock block, ItemModel itemModel, Vec2? blockSizeForRotation)
    {
        var id = string.Format(itemModel.Id ?? throw new Exception("ItemModel ID not available"), GetBlockStringArgs(block));

        var ident = new Ident(id,
            itemModel.Collection ?? conversions.DefaultCollection ?? map.Collection ?? throw new Exception("Map collection is null."),
            itemModel.Author ?? conversions.DefaultAuthor ?? "");

        var dirOffset = 0;

        var absolutePosition = (block.Coord - (0, conversions.DecoBaseHeight, 0)) * blockSize + blockSize * (1, 0, 1) * 0.5f;
        var pitchYawRoll = new Vec3(-((int)block.Direction - dirOffset) * MathF.PI / 2, 0, 0);

        if (blockSizeForRotation.HasValue)
        {
            blockSizeForRotation = new Vec2(blockSizeForRotation.Value.X - 1, blockSizeForRotation.Value.Y - 1);

            switch (block.Direction)
            {
                case Direction.East:
                    absolutePosition += (blockSizeForRotation.Value.Y * blockSize.X, 0, 0);
                    break;
                case Direction.South:
                    absolutePosition += (blockSizeForRotation.Value.X * blockSize.X, 0, blockSizeForRotation.Value.Y * blockSize.Z);
                    break;
                case Direction.West:
                    absolutePosition += (0, 0, blockSizeForRotation.Value.X * blockSize.X);
                    break;
            }
        }

        map.PlaceAnchoredObject(ident, absolutePosition, pitchYawRoll, -itemModel.Pivot ?? -blockSize * (1, 0, 1) * 0.5f);
    }
}

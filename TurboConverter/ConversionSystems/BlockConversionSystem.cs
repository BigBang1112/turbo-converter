using GBX.NET;
using GBX.NET.Engines.Game;
using GBX.NET.Engines.Script;
using TurboConverter.Extensions;
using TurboConverter.Models;

namespace TurboConverter.ConversionSystems;

sealed class BlockConversionSystem : IConversionSystem
{
    private readonly CGameCtnChallenge map;
    private readonly Conversions conversions;
    private readonly Converters converters;
    private readonly Int3 blockSize;
    private readonly ILookup<Int3, CGameCtnBlock> blocksByCoord;
    private readonly HashSet<CGameCtnBlockSkin> modifiedSkins = new();
    
    public IList<CScriptTraitsMetadata.ScriptStructTrait> ConvertedBlocks { get; } = new List<CScriptTraitsMetadata.ScriptStructTrait>();

    public BlockConversionSystem(CGameCtnChallenge map, Conversions conversions, Converters converters)
    {
        this.map = map;
        this.conversions = conversions;
        this.converters = converters;

        blockSize = map.Collection.GetBlockSize();

        _ = map.Blocks ?? throw new Exception("Map blocks are null.");
        blocksByCoord = map.Blocks.ToLookup(x => x.Coord);
    }

    public void Run()
    {
        _ = map.Blocks ?? throw new Exception("Map blocks are null.");

        // Backwards loop to allow for block removal
        for (var i = map.Blocks.Count - 1; i >= 0; i--)
        {
            var block = map.Blocks[i];

            // Boosts performance
            if (block.Name == "Unassigned1")
            {
                continue;
            }

            ApplyBlockConversion(block, i);
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
                return;
            }

            var removeBlock = true;

            var variant = block.Variant.GetValueOrDefault();

            if (conversion.Length > variant)
            {
                ApplyBlockConversion(block, blockIndex, conversion[variant], out removeBlock);
            }
            else
            {
                Console.WriteLine($"Block {block.Name} has variant {variant} but only {conversion.Length} variants are defined. Block will be removed.");
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

        if (conversion.VariantOf is not null)
        {
            var blockForVariant = blocksByCoord[block.Coord].FirstOrDefault(x => conversion.VariantOf.ContainsKey(x.Name));

            if (blockForVariant is not null)
            {
                ApplyBlockConversion(block, blockIndex, conversion.VariantOf[blockForVariant.Name][blockForVariant.Variant.GetValueOrDefault()], out removeBlock);
                return;
            }
        }

        if (!string.IsNullOrEmpty(conversion.Converter))
        {
            if (!converters.BlockConverters.TryGetValue(conversion.Converter, out var converter))
            {
                throw new Exception($"Converter {conversion.Converter} does not exist.");
            }

            if (converter is null)
            {
                throw new Exception($"Converter {conversion.Converter} is not implemented.");
            }

            if (converter.ItemModel is not null)
            {
                PlaceAnchoredObject(block, converter.ItemModel, conversion.Size);
            }

            if (converter.Name is not null)
            {
                block.Name = converter.Name.Apply(block.Name, conversion.Converter);
            }

            if (block.Skin is not null && !string.IsNullOrEmpty(block.Skin.PackDesc.FilePath) && converter.Skin is not null && !modifiedSkins.Contains(block.Skin))
            {
                block.Skin.PackDesc = block.Skin.PackDesc with { FilePath = converter.Skin.Apply(block.Skin.PackDesc.FilePath, conversion.Converter) };
                modifiedSkins.Add(block.Skin);
            }

            removeBlock = false;
        }

        if (!string.IsNullOrEmpty(conversion.Name))
        {
            block.Name = conversion.Name;
            removeBlock = false;
        }

        if (conversion.Variant.HasValue)
        {
            block.Variant = (byte?)conversion.Variant;
            removeBlock = false;
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

        if (conversion.SubVariants?.Length > 0)
        {
            ApplyBlockConversion(block, blockIndex, conversion.SubVariants[block.SubVariant.GetValueOrDefault()], out removeBlock);
        }
    }

    private void PlaceAnchoredObject(CGameCtnBlock block, ItemModel itemModel, Vec2? blockSizeForRotation)
    {
        var id = string.Format(itemModel.Id ?? throw new Exception("ItemModel ID not available"),
            block.Name,
            map.Collection,
            block.IsGround ? "Ground" : "Air",
            Path.GetFileNameWithoutExtension(block.Skin?.PackDesc.FilePath) ?? "WELCOME_TM");

        var ident = new Ident(id,
            itemModel.Collection ?? conversions.DefaultCollection ?? map.Collection,
            itemModel.Author ?? conversions.DefaultAuthor ?? "");

        var absolutePosition = (block.Coord - (0, conversions.DecoBaseHeight, 0)) * blockSize + blockSize.GetXZ() * 0.5f;
        var pitchYawRoll = new Vec3(-(int)block.Direction * MathF.PI / 2, 0, 0);

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

        map.PlaceAnchoredObject(ident, absolutePosition, pitchYawRoll, -itemModel.Pivot ?? -blockSize.GetXZ() * 0.5f);
    }
}

using GBX.NET;
using GBX.NET.Engines.Game;
using TurboConverter.Extensions;
using TurboConverter.Models;

namespace TurboConverter.ConversionSystems;

sealed class BlockConversionSystem : IConversionSystem
{
    private readonly CGameCtnChallenge map;
    private readonly Conversions conversions;
    private readonly Converters converters;
    private readonly Int3 blockSize;

    public BlockConversionSystem(CGameCtnChallenge map, Conversions conversions, Converters converters)
    {
        this.map = map;
        this.conversions = conversions;
        this.converters = converters;

        blockSize = map.Collection.GetBlockSize();
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
        if (conversions.Blocks.TryGetValue(block.Name, out var conversion))
        {
            ApplyBlockConversion(block, blockIndex, conversion);
        }
    }

    private bool ApplyBlockConversion(CGameCtnBlock block, int blockIndex, BlockConversion? conversion)
    {
        if (conversion is null)
        {
            map.RemoveBlockAt(blockIndex);
            return false;
        }

        var removeBlock = true;

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
            var ident = new Ident(conversion.ItemModel.Id ?? throw new Exception("ItemModel ID not available"),
                conversion.ItemModel.Collection ?? conversions.DefaultCollection ?? map.Collection,
                conversion.ItemModel.Author ?? conversions.DefaultAuthor ?? "");

            map.PlaceAnchoredObject(ident, (block.Coord - (0, conversions.DecoBaseHeight, 0)) * blockSize + blockSize * 0.5f, new(), -conversion.ItemModel.Pivot ?? new());
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

            if (converter.Name is not null)
            {
                block.Name = converter.Name.Apply(block.Name, conversion.Converter);
            }

            removeBlock = false;
        }

        if (removeBlock)
        {
            map.RemoveBlockAt(blockIndex);
        }

        return !removeBlock;
    }
}

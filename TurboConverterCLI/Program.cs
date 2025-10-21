using GBX.NET;
using GBX.NET.Hashing;
using GBX.NET.Tool.CLI;
using TurboConverter;
using TurboConverter.YmlConverters;
using YamlDotNet.Serialization;

Gbx.CRC32 = new CRC32();

await ToolConsole<TurboConverterTool>.RunAsync(args, new()
{
    YmlDeserializer = new DeserializerBuilder()
        .WithTypeConverter(new YmlIdConverter())
        .WithTypeConverter(new YmlVec2Converter())
        .WithTypeConverter(new YmlVec3Converter()),
    YmlSerializer = new SerializerBuilder(),
    GitHubRepo = "bigbang1112/turbo-converter"
});
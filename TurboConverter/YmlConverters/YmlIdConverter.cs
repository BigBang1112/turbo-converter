using GBX.NET;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

namespace TurboConverter.YmlConverters;

public sealed class YmlIdConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(Id) || type == typeof(Id?);

    public object? ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var scalar = parser.Consume<Scalar>();

        if (int.TryParse(scalar.Value, out int intValue))
        {
            return new Id(intValue);
        }
        else
        {
            return new Id(scalar.Value);
        }
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type, ObjectSerializer serializer)
    {

    }
}
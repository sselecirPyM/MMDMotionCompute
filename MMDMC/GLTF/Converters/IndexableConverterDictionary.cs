using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MMDMC.GLTF.Converters
{
    public class IndexableConverterDictionary<T, T2> : JsonConverter<IReadOnlyDictionary<T, T2>> where T2 : IndexableObject
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsAssignableTo(typeof(IReadOnlyDictionary<T, T2>));
        }

        public override IReadOnlyDictionary<T, T2> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<T, T2> value, JsonSerializerOptions options)
        {
            if (value == null)
                return;

            var op1 = new JsonSerializerOptions(options);
            op1.Converters.Add(IndexableConverter.instance);
            writer.WriteStartObject();
            foreach (var item in value)
            {
                writer.WritePropertyName(item.Key.ToString());
                JsonSerializer.Serialize(writer, item.Value, op1);
            }
            writer.WriteEndObject();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MMDMC.GLTF.Converters
{
    public class IndexableConverterArray : JsonConverter<IEnumerable<IndexableObject>>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsAssignableTo(typeof(IEnumerable<IndexableObject>));
        }

        public override IEnumerable<IndexableObject> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, IEnumerable<IndexableObject> value, JsonSerializerOptions options)
        {
            if (value == null)
                return;

            var op1 = new JsonSerializerOptions(options);
            op1.Converters.Add(IndexableConverter.instance);
            writer.WriteStartArray();
            foreach (var item in value)
            {
                JsonSerializer.Serialize(writer, item, op1);
            }
            writer.WriteEndArray();
        }
    }
    public class IndexableConverterArray2<T, T2> : JsonConverter<IEnumerable<object>> where T2 : IndexableObject
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsAssignableTo(typeof(IEnumerable<object>));
        }

        public override IEnumerable<object> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, IEnumerable<object> value, JsonSerializerOptions options)
        {
            if (value == null)
                return;

            var op1 = new JsonSerializerOptions(options);
            op1.Converters.Add(new IndexableConverterDictionary<T, T2>());
            writer.WriteStartArray();
            foreach (var item in value)
            {
                JsonSerializer.Serialize(writer, item, op1);
            }
            writer.WriteEndArray();
        }
    }
}

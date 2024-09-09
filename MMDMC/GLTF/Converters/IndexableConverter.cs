using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MMDMC.GLTF.Converters
{
    public class IndexableConverter : JsonConverter<IndexableObject>
    {
        public static IndexableConverter instance = new IndexableConverter();
        public override bool CanConvert(Type typeToConvert)
        {
            return Type.IsAssignableTo(typeof(IndexableObject));
        }
        public override IndexableObject Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, IndexableObject value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value._Index);
        }
    }
    //public class IndexableConverterFactory : JsonConverterFactory
    //{
    //    public override bool CanConvert(Type typeToConvert)
    //    {
    //        if (typeToConvert.IsAssignableTo(typeof(IndexableObject)))
    //        {
    //            return true;
    //        }

    //        if (typeToConvert.IsGenericType && typeToConvert.IsAssignableTo(typeof(IEnumerable<IndexableObject>)))
    //        {
    //            return true;
    //        }

    //        return false;
    //    }

    //    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}

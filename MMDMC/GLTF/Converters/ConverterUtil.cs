using System.Numerics;
using System.Text.Json;

namespace MMDMC.GLTF.Converters
{
    public static class ConverterUtil
    {
        public static void WriteArray(this Utf8JsonWriter writer, string name, float[] array)
        {
            if (array == null)
                return;
            writer.WriteStartArray(name);
            foreach (float value in array)
            {
                writer.WriteNumberValue(value);
            }
            writer.WriteEndArray();
        }
        public static void WriteVector(this Utf8JsonWriter writer, string name, Vector2? vector2)
        {
            if (vector2 == null)
                return;
            writer.WriteStartArray(name);
            writer.WriteNumberValue(vector2.Value.X);
            writer.WriteNumberValue(vector2.Value.Y);
            writer.WriteEndArray();
        }
        public static void WriteVector(this Utf8JsonWriter writer, string name, Vector3? vector3)
        {
            if (vector3 == null)
                return;
            writer.WriteStartArray(name);
            writer.WriteNumberValue(vector3.Value.X);
            writer.WriteNumberValue(vector3.Value.Y);
            writer.WriteNumberValue(vector3.Value.Z);
            writer.WriteEndArray();
        }
    }
}

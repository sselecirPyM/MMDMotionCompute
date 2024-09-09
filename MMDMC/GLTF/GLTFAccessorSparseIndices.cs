using MMDMC.GLTF.Converters;
using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFAccessorSparseIndices
    {
        [JsonConverter(typeof(IndexableConverter))]
        public GLTFBufferView bufferView { get; set; }
        public int byteOffset { get; set; }
        public int componentType { get; set; }
    }
}

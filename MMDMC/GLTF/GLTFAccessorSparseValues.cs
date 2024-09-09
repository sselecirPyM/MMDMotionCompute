using MMDMC.GLTF.Converters;
using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFAccessorSparseValues
    {
        [JsonConverter(typeof(IndexableConverter))]
        public GLTFBufferView bufferView { get; set; }
        public int byteOffset { get; set; }
    }
}

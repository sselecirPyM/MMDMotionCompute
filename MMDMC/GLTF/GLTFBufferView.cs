using MMDMC.GLTF.Converters;
using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFBufferView : IndexableObject
    {
        [JsonConverter(typeof(IndexableConverter))]
        public GLTFBuffer buffer { get; set; }
        public int? byteOffset { get; set; }
        public int byteLength { get; set; }
        public int? byteStride { get; set; }
        public int? target { get; set; }
        public string name { get; set; }

        [JsonIgnore]
        public int _Index { get; set; }
    }
}

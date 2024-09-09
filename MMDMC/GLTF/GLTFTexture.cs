using MMDMC.GLTF.Converters;
using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFTexture : IndexableObject
    {
        public int? sampler { get; set; }
        [JsonConverter(typeof(IndexableConverter))]
        public GLTFImage source { get; set; }

        [JsonIgnore]
        public int _Index { get; set; }
    }
}

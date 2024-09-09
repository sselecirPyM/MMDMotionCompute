using MMDMC.GLTF.Converters;
using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFAnimationSampler : IndexableObject
    {
        [JsonConverter(typeof(IndexableConverter))]
        public GLTFAccessor input { get; set; }
        public string interpolation { get; set; }
        [JsonConverter(typeof(IndexableConverter))]
        public GLTFAccessor output { get; set; }

        [JsonIgnore]
        public int _Index { get; set; }
    }
}

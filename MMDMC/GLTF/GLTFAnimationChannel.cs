using MMDMC.GLTF.Converters;
using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFAnimationChannel
    {
        [JsonConverter(typeof(IndexableConverter))]
        public GLTFAnimationSampler sampler { get; set; }
        public GLTFAnimationChannelTarget target { get; set; }
    }
}

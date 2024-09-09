using MMDMC.GLTF.Converters;
using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFAnimationChannelTarget
    {
        [JsonConverter(typeof(IndexableConverter))]
        public GLTFNode node { get; set; }
        public string path { get; set; }
    }
}

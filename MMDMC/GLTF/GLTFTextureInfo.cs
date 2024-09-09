using MMDMC.GLTF.Converters;
using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFTextureInfo
    {
        [JsonConverter(typeof(IndexableConverter))]
        public GLTFTexture index { get; set; }
        public int? texCoord { get; set; }
    }
}

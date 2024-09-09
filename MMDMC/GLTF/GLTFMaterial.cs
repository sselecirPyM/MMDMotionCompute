using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFMaterial : IndexableObject
    {
        public string name { get; set; }
        public GLTFPBRMetallicRoughness pbrMetallicRoughness { get; set; }
        public bool? doubleSided { get; set; }

        [JsonIgnore]
        public int _Index { get; set; }
    }
}

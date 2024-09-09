using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFMesh : IndexableObject
    {
        public string name { get; set; }
        public GLTFPrimitive[] primitives { get; set; }

        [JsonIgnore]
        public int _Index { get; set; }
    }
}

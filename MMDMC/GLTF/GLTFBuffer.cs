using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFBuffer : IndexableObject
    {
        public string uri { get; set; }
        public int? byteLength { get; set; }
        public string name { get; set; }

        [JsonIgnore]
        public int _Index { get; set; }
    }
}

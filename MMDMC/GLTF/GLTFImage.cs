using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFImage : IndexableObject
    {
        public string uri { get; set; }
        public string mimeType { get; set; }
        public int? bufferView { get; set; }
        public string name { get; set; }

        [JsonIgnore]
        public int _Index { get; set; }
    }
}

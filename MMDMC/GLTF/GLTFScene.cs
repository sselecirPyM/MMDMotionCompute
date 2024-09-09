using MMDMC.GLTF.Converters;
using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFScene
    {
        public string name { get; set; }

        [JsonConverter(typeof(IndexableConverterArray))]
        public GLTFNode[] nodes { get; set; }
    }
}

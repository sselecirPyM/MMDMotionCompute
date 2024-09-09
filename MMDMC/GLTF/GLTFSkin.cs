using MMDMC.GLTF.Converters;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFSkin : IndexableObject
    {
        public string name { get; set; }
        [JsonConverter(typeof(IndexableConverter))]
        public GLTFAccessor inverseBindMatrices { get; set; }

        [JsonConverter(typeof(IndexableConverterArray))]
        public List<GLTFNode> joints { get; set; }
        [JsonConverter(typeof(IndexableConverter))]
        public GLTFNode skeleton { get; set; }

        [JsonIgnore]
        public int _Index { get; set; }
    }
}

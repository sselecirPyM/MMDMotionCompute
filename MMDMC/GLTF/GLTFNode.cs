using MMDMC.GLTF.Converters;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFNode: IndexableObject
    {
        public string name { get; set; }
        public int? camera { get; set; }
        public float[] translation { get; set; }
        public float[] rotation { get; set; }
        public float[] scale { get; set; }
        public float[] matrix { get; set; }
        [JsonConverter(typeof(IndexableConverter))]
        public GLTFMesh mesh { get; set; }
        [JsonConverter(typeof(IndexableConverter))]
        public GLTFSkin skin { get; set; }

        [JsonIgnore]
        public int _Index { get; set; }

        [JsonConverter(typeof(IndexableConverterArray))]
        public List<GLTFNode> children { get; set; }
    }
}

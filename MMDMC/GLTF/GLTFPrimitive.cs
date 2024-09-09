using MMDMC.GLTF.Converters;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFPrimitive
    {
        [JsonConverter(typeof(IndexableConverterDictionary<string, GLTFAccessor>))]
        public Dictionary<string, GLTFAccessor> attributes { get; set; }

        [JsonConverter(typeof(IndexableConverterArray2<string, GLTFAccessor>))]
        public Dictionary<string, GLTFAccessor>[] targets { get; set; }

        [JsonConverter(typeof(IndexableConverter))]
        public GLTFAccessor indices { get; set; }
        [JsonConverter(typeof(IndexableConverter))]
        public GLTFMaterial material { get; set; }
        public int? mode { get; set; }
        public string name { get; set; }
    }
}

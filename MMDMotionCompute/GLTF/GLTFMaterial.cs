using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMDMotionCompute.GLTF
{
    public class GLTFMaterial
    {
        public string name { get; set; }
        public GLTFPBRMetallicRoughness pbrMetallicRoughness { get; set; }
        public bool? doubleSided { get; set; }
    }
}

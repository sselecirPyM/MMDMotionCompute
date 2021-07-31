using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMDMotionCompute.GLTF
{
    public class GLTFMesh
    {
        public string name { get; set; }
        public GLTFPrimitive[] primitives { get; set; }
    }
}

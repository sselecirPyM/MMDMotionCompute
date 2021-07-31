using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMDMotionCompute.GLTF
{
    public class GLTFAccessor
    {
        public int bufferView { get; set; }
        public int byteOffset { get; set; }
        public int componentType { get; set; }
        public bool? normalized { get; set; }
        public int count { get; set; }
        public string type { get; set; }
        public float[] max { get; set; }
        public float[] min { get; set; }
        public string name { get; set; }

    }
}

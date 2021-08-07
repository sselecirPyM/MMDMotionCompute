using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMDMotionCompute.GLTF
{
    public class GLTFBufferView
    {
        public int buffer { get; set; }
        public int? byteOffset { get; set; }
        public int byteLength { get; set; }
        public int? byteStride { get; set; }
        public int? target { get; set; }
        public string name { get; set; }
    }
}

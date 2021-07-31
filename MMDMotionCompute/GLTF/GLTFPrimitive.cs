using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMDMotionCompute.GLTF
{
    public class GLTFPrimitive
    {
        public Dictionary<string,int> attributes { get; set; }
        public Dictionary<string,int>[] targets { get; set; }
        public int indices { get; set; }
        public int material { get; set; }
        public int? mode { get; set; }
        public string name { get; set; }
    }
}

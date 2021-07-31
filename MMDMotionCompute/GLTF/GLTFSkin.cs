using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMDMotionCompute.GLTF
{
    public class GLTFSkin
    {
        public string name { get; set; }
        public int? inverseBindMatrices { get; set; }
        public int[] joints { get; set; }
        public int? skeleton { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMDMotionCompute.GLTF
{
    public class GLTFAccessorSparse
    {
        public int count { get; set; }
        public GLTFAccessorSparseIndices indices { get; set; }
        public GLTFAccessorSparseValues values { get; set; }
    }
}

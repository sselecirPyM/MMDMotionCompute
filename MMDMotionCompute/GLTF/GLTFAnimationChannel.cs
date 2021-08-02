using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMDMotionCompute.GLTF
{
    public class GLTFAnimationChannel
    {
        public int sampler { get; set; }
        public GLTFAnimationChannelTarget target { get; set; }
    }
}

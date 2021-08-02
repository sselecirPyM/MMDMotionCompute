using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMDMotionCompute.GLTF
{
    public class GLTFAnimation
    {
        public string name { get; set; }
        public GLTFAnimationChannel[] channels { get; set; }
        public GLTFAnimationSampler[] samplers { get; set; }
    }
}

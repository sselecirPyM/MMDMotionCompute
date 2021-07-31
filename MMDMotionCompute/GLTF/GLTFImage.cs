using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMDMotionCompute.GLTF
{
    public class GLTFImage
    {
        public string uri { get; set; }
        public string mimeType { get; set; }
        public int? bufferView { get; set; }
        public string name { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMDMotionCompute.GLTF;

namespace MMDMotionCompute.Functions
{
    public class GLTFWriterContext
    {
        public Dictionary<string, int> accessorStart = new Dictionary<string, int>();
        public void StartWriteAccessorMark(string name)
        {
            accessorStart.Add(name, accessors.Count);
        }
        public List<GLTFAccessor> accessors = new List<GLTFAccessor>();

        public Dictionary<string, int> bufferOffset = new Dictionary<string, int>();
        public Dictionary<string, int> bufferLength = new Dictionary<string, int>();
    }
}

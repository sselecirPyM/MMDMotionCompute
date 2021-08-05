using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMDMotionCompute.GLTF;
using MMDMotionCompute.Utility;

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
        public List<GLTFBufferView> bufferViews = new List<GLTFBufferView>();
        public Dictionary<string, int> bufferViewStart = new Dictionary<string, int>();
        public Dictionary<string, OffsetAndLength> bufferOffset = new Dictionary<string, OffsetAndLength>();

        public int streamPosition = 0;

        public void MarkBuf(string name)
        {
            int streamCurrentposition = (int)writer.BaseStream.Position;
            int length = streamCurrentposition - streamPosition;
            bufferOffset[name] = new OffsetAndLength(streamPosition, length, 0);

            streamPosition = (int)writer.BaseStream.Position;
        }

        public BinaryWriterPlus writer;

    }
    public struct OffsetAndLength
    {
        public int offset;
        public int length;
        public int localOffset;

        public OffsetAndLength(int offset, int length)
        {
            this.offset = offset;
            this.length = length;
            this.localOffset = 0;
        }

        public OffsetAndLength(int offset, int length, int localOffset)
        {
            this.offset = offset;
            this.length = length;
            this.localOffset = localOffset;
        }
    }
}

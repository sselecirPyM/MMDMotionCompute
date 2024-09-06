using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMDMC.GLTF;
using MMDMC.Utility;

namespace MMDMC.Functions
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

        public void MarkBuf(string name, GLTFBufferView desc, GLTFAccessor desc1)
        {
            int streamCurrentposition = (int)writer.BaseStream.Position;
            int length = streamCurrentposition - streamPosition;
            bufferOffset[name] = new OffsetAndLength(streamPosition, length, 0);
            if (desc != null)
            {
                bufferViewStart[name] = bufferViews.Count;
                desc.buffer = 0;
                desc.byteLength = length;
                desc.byteOffset = streamPosition;
                desc.name = name;
                bufferViews.Add(desc);
            }
            if (desc1 != null)
            {
                accessorStart[name] = accessors.Count;
                desc1.bufferView = bufferViews.Count - 1;
                desc1.byteOffset = 0;
                desc1.name = name;
                accessors.Add(desc1);

            }
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

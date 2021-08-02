using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMDMotionCompute.GLTF
{
    public class GLTFModel
    {
        public GLTFAsset asset { get; set; } = new GLTFAsset();
        public GLTFAccessor[] accessors { get; set; }
        public int scene { get; set; }
        public GLTFScene[] scenes { get; set; }
        public GLTFBuffer[] buffers { get; set; }
        public GLTFBufferView[] bufferViews { get; set; }
        public GLTFImage[] images { get; set; }
        public GLTFMaterial[] materials { get; set; }
        public GLTFMesh[] meshes { get; set; }
        public GLTFNode[] nodes { get; set; }
        public GLTFSkin[] skins { get; set; }
        public GLTFTexture[] textures { get; set; }
        public GLTFAnimation[] animations { get; set; }
    }
}

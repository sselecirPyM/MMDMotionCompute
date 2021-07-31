using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMDMotionCompute.GLTF
{
    public class GLTFPBRMetallicRoughness
    {
        public GLTFTextureInfo baseColorTexture { get; set; }
        public float? metallicFactor { get; set; }
        public float? roughnessFactor { get; set; }
        public GLTFTextureInfo metallicRoughnessTexture { get; set; }
    }
}

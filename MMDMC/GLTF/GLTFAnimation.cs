using System.Text.Json.Serialization;

namespace MMDMC.GLTF
{
    public class GLTFAnimation
    {
        public string name { get; set; }
        public GLTFAnimationChannel[] channels { get; set; }
        public GLTFAnimationSampler[] samplers { get; set; }
    }
}

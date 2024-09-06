namespace MMDMC.GLTF
{
    public class GLTFMaterial
    {
        public string name { get; set; }
        public GLTFPBRMetallicRoughness pbrMetallicRoughness { get; set; }
        public bool? doubleSided { get; set; }
    }
}

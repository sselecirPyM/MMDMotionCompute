namespace MMDMC.GLTF
{
    public class GLTFPBRMetallicRoughness
    {
        public GLTFTextureInfo baseColorTexture { get; set; }
        public float? metallicFactor { get; set; }
        public float? roughnessFactor { get; set; }
        public GLTFTextureInfo metallicRoughnessTexture { get; set; }
    }
}

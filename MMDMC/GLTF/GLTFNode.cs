namespace MMDMC.GLTF
{
    public class GLTFNode
    {
        public string name { get; set; }
        public int[] children { get; set; }
        public int? camera { get; set; }
        public int? mesh { get; set; }
        public float[] rotation { get; set; }
        public float[] scale { get; set; }
        public float[] translation { get; set; }
        public float[] matrix { get; set; }
        public int? skin { get; set; }
    }
}

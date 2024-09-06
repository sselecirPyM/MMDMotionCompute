using CommandLine;

namespace MMDMC
{
    public class ExportOptions
    {
        [Option(Required = true)]
        public string model { get; set; }
        [Option]
        public string motion { get; set; }

        [Option(Required = true)]
        public string output { get; set; }

        [Option]
        public int frameRate { get; set; } = 30;
        [Option]
        public bool usePhysics { get; set; } = true;
        [Option]
        public bool sparseMorph { get; set; } = true;
        [Option]
        public float exportScale { get; set; } = 0.1f;
        [Option]
        public float gravity { get; set; } = 100.0f;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMDMotionCompute.MMD
{
    public class VMDSampler1
    {
        public static List<float[]> ProcessVertMorph(VMDFormat vmd, IReadOnlyList<PMX_Morph> vertMorphs, out float[] frames)
        {
            int lastFrame = 0;
            HashSet<int> frameIndexs = new HashSet<int>();
            foreach (var a in vmd.MorphKeyFrameSet)
            {
                foreach (var b in a.Value)
                {
                    lastFrame = Math.Max(b.Frame, lastFrame);
                    frameIndexs.Add(b.Frame);
                }
            }
            var frameIndexs1 = frameIndexs.ToArray();
            Array.Sort(frameIndexs1);

            List<float[]> process = new List<float[]>();
            MMDMotion x1 = VMDFormatExtension.Load(vmd);
            for (int i = 0; i < frameIndexs1.Length; i++)
            {
                int t = frameIndexs1[i];
                float[] weights = new float[vertMorphs.Count];
                for (int j = 0; j < vertMorphs.Count; j++)
                {
                    weights[j] = x1.GetMorphWeight(vertMorphs[j].Name, t / 30.0f);
                }


                process.Add(weights);
            }
            frames = frameIndexs1.Select(u => u / 30.0f).ToArray();
            return process;
        }
    }
}

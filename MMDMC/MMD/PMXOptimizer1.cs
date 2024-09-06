//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace MMDMC.MMD
//{
//    public class PMXOptimizer1
//    {
//        /// <summary>
//        /// 功能：优化顶点和索引，使材质索引的最小值和最大值之差最小，且不同材质间不共享顶点。以方便将不同材质拆分成独立的网格。
//        /// </summary>
//        /// <param name="pmx"></param>
//        /// <returns></returns>
//        public static PMXFormat OptimizePMX(PMXFormat pmx)
//        {
//            PMXFormat pmxOutput = new PMXFormat
//            {
//                Bones = pmx.Bones,
//                Description = pmx.Description,
//                DescriptionEN = pmx.DescriptionEN,
//                Entries = pmx.Entries,
//                Joints = pmx.Joints,
//                Materials = pmx.Materials.Select(u => u.Clone()).ToList(),
//                Morphs = pmx.Morphs.Select(u => u.Clone()).ToList(),
//                Name = pmx.Name,
//                NameEN = pmx.NameEN,
//                Ready = pmx.Ready,
//                RigidBodies = pmx.RigidBodies,
//                Textures = pmx.Textures,
//                TriangleIndexs = pmx.TriangleIndexs.ToArray(),
//                //Vertices = pmx.Vertices.ToArray(),
//            };

//            var materials = pmxOutput.Materials;
//            var oldIndices = pmx.TriangleIndexs;
//            int vertC = 0;
//            List<int> new2Old = new List<int>();
//            int[] newIndices = new int[oldIndices.Length];

//            //获取变形目标对顶点网格的引用
//            Dictionary<int, List<MorphInvertRef>> refMap = new Dictionary<int, List<MorphInvertRef>>();
//            Dictionary<int, List<PMX_MorphVertexDesc>> newVertMorph = new Dictionary<int, List<PMX_MorphVertexDesc>>();
//            for (int i = 0; i < pmxOutput.Morphs.Count; i++)
//            {
//                var morph = pmxOutput.Morphs[i];
//                if (morph.MorphVertice != null)
//                {
//                    RefMapRecord(refMap, morph, i);
//                    newVertMorph[i] = new List<PMX_MorphVertexDesc>();
//                }
//            }

//            for (int i = 0; i < materials.Count; i++)
//            {
//                var mat = materials[i];
//                Range range = new Range(mat.TriangeIndexStartNum, mat.TriangeIndexNum + mat.TriangeIndexStartNum);
//                var indicesSet = oldIndices[range].ToHashSet();
//                Dictionary<int, int> old2New = new Dictionary<int, int>();
//                foreach (var k in indicesSet)
//                {
//                    old2New[k] = vertC;
//                    new2Old.Add(k);
//                    if (refMap.TryGetValue(k, out var refC))
//                    {
//                        foreach (var morphRef in refC)
//                        {
//                            var vertRef1 = morphRef.vertRef;
//                            vertRef1.VertexIndex = vertC;
//                            newVertMorph[morphRef.indexRef].Add(vertRef1);
//                        }
//                    }
//                    vertC++;
//                }
//                //将旧索引映射到新索引
//                for (int j = mat.TriangeIndexStartNum; j < mat.TriangeIndexNum + mat.TriangeIndexStartNum; j++)
//                {
//                    int oldI = oldIndices[j];
//                    int newI = old2New[oldI];
//                    newIndices[j] = newI;
//                }
//            }
//            pmxOutput.TriangleIndexs = newIndices;
//            //变形引用更新
//            foreach (var pair in newVertMorph)
//            {
//                pair.Value.Sort((x, y) => { return x.VertexIndex.CompareTo(y.VertexIndex); });
//                pmxOutput.Morphs[pair.Key].MorphVertice = pair.Value.ToArray();
//            }
//            //for (int i = 0; i < pmxOutput.Morphs.Count; i++)
//            //{
//            //    var morph = pmxOutput.Morphs[i];
//            //    if (morph.MorphVertexs != null)
//            //    {
//            //        Array.Sort(morph.MorphVertexs, (x, y) => x.VertexIndex.CompareTo(y.VertexIndex));
//            //    }
//            //}

//            //拷贝旧顶点数组到新顶点数组
//            pmxOutput.Vertices = new PMX_Vertex[new2Old.Count];
//            for (int i = 0; i < new2Old.Count; i++)
//            {
//                pmxOutput.Vertices[i] = pmx.Vertices[new2Old[i]];
//            }

//            return pmxOutput;
//        }
//        //记录引用
//        static void RefMapRecord(Dictionary<int, List<MorphInvertRef>> refMap, PMX_Morph vertMorph, int index)
//        {
//            for (int i = 0; i < vertMorph.MorphVertice.Length; i++)
//            {
//                int c = vertMorph.MorphVertice[i].VertexIndex;
//                if (!refMap.TryGetValue(c, out var d))
//                {
//                    d = new List<MorphInvertRef>();
//                    refMap[c] = d;
//                }
//                d.Add(new MorphInvertRef { indexRef = index, vertRef = vertMorph.MorphVertice[i] });
//            }
//        }

//        struct MorphInvertRef
//        {
//            public int indexRef;
//            public PMX_MorphVertexDesc vertRef;
//        }
//    }
//}

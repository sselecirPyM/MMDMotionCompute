using MMDMC.GLTF;
using MMDMC.MMD;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MMDMC.Functions
{
    public class ModelContext
    {
        public PMXFormat pmx;
        public VMDFormat vmd;

        public GLTFWriterContext GLTFWriterContext;
        public List<GLTFNode> gltfBones;
        public GLTFSkin gltfSkin;

        public Dictionary<string, GLTFNode> name2Bone;

        public GLTFImage[] images;
        public GLTFTexture[] textures;
        public GLTFMaterial[] materials;

        public List<GLTFMesh> meshes;

        Dictionary<PMX_Material, GLTFMaterial> mat2mat;

        HashSet<int> morphIndices;

        PMX_Morph[] vertexMorphs;

        public void CollectMaterials()
        {
            images = new GLTFImage[pmx.Textures.Count];
            textures = new GLTFTexture[pmx.Textures.Count];
            materials = new GLTFMaterial[pmx.Materials.Count];
            mat2mat = new Dictionary<PMX_Material, GLTFMaterial>();

            for (int i = 0; i < pmx.Textures.Count; i++)
            {
                var img = new GLTFImage { uri = pmx.Textures[i].TexturePath.Replace('\\', '/') };
                images[i] = img;
                textures[i] = new GLTFTexture() { source = img };
            }

            for (int i = 0; i < pmx.Materials.Count; i++)
            {
                var mat = pmx.Materials[i];
                materials[i] = new GLTFMaterial
                {
                    name = mat.Name,
                    pbrMetallicRoughness = new GLTFPBRMetallicRoughness { metallicFactor = 0.0f, roughnessFactor = 0.0f },
                    doubleSided = mat.DrawFlags.HasFlag(PMX_DrawFlag.DrawDoubleFace)
                };
                mat2mat[mat] = materials[i];
                if (mat.TextureIndex >= 0)
                    materials[i].pbrMetallicRoughness.baseColorTexture = new GLTFTextureInfo { index = textures[mat.TextureIndex] };
            }

            morphIndices = new HashSet<int>();
            foreach (var morph in pmx.Morphs)
            {
                if (morph.MorphVertice != null)
                {
                    foreach (var a in morph.MorphVertice)
                    {
                        morphIndices.Add(a.VertexIndex);
                    }
                }
            }


            vertexMorphs = pmx.Morphs.Where(u => u.MorphVertice != null).ToArray();
        }

        public List<GLTFNode> GetBones(Matrix4x4 exportTransform)
        {
            gltfBones = new List<GLTFNode>();
            name2Bone = new Dictionary<string, GLTFNode>();
            for (int i = 0; i < pmx.Bones.Count; i++)
            {
                PMX_Bone bone = pmx.Bones[i];
                var thisBone = new GLTFNode { name = pmx.Bones[i].Name };
                gltfBones.Add(thisBone);
                name2Bone[bone.Name] = thisBone;
                Vector3 relatePosition = bone.Position;
                if (bone.ParentIndex >= 0 && bone.ParentIndex < pmx.Bones.Count)
                {
                    gltfBones[bone.ParentIndex].children ??= new List<GLTFNode>();
                    gltfBones[bone.ParentIndex].children.Add(thisBone);
                    relatePosition -= pmx.Bones[bone.ParentIndex].Position;
                }

                relatePosition = Vector3.Transform(relatePosition, exportTransform);
                thisBone.translation = new float[3] { relatePosition.X, relatePosition.Y, relatePosition.Z, };
            }

            return gltfBones;
        }

        public GLTFSkin GetSkin(Matrix4x4 exportTransform, GLTFWriterContext glTFWriterContext)
        {
            for (int i = 0; i < pmx.Bones.Count; i++)
            {
                glTFWriterContext.writer.Write(Matrix4x4.CreateTranslation(Vector3.Transform(-pmx.Bones[i].Position, exportTransform)));
            }
            glTFWriterContext.CreateAccessor("_inverseBindMatrices", new GLTFAccessor { componentType = 5126, count = pmx.Bones.Count, type = "MAT4" });

            var skin = new GLTFSkin();
            List<GLTFNode> joints = new List<GLTFNode>();
            for (int i = 0; i < gltfBones.Count; i++)
            {
                joints.Add(gltfBones[i]);
            }
            skin.joints = joints;
            skin.inverseBindMatrices = glTFWriterContext.accessorStart["_inverseBindMatrices"];
            skin.skeleton = gltfBones[0];
            gltfSkin = skin;
            return skin;
        }

        public GLTFPrimitive CreatePrimitive(Matrix4x4 exportTransform, PMX_Material material, bool useMorph)
        {
            int indexStart = material.TriangeIndexStartNum;
            List<int> modifiedIndex = new List<int>();

            for (int i = 0; i < material.TriangeIndexNum; i += 3)
            {
                int i1 = i + indexStart;
                bool anyBlend = false;
                anyBlend |= morphIndices.Contains(pmx.TriangleIndexs[i1]);
                anyBlend |= morphIndices.Contains(pmx.TriangleIndexs[i1 + 1]);
                anyBlend |= morphIndices.Contains(pmx.TriangleIndexs[i1 + 2]);
                if (anyBlend == useMorph)
                {
                    modifiedIndex.Add(pmx.TriangleIndexs[i1]);
                    modifiedIndex.Add(pmx.TriangleIndexs[i1 + 1]);
                    modifiedIndex.Add(pmx.TriangleIndexs[i1 + 2]);
                }
            }
            if (modifiedIndex.Count == 0)
                return null;
            var c1 = new HashSet<int>();

            for (int i = 0; i < modifiedIndex.Count; i++)
            {
                c1.Add(modifiedIndex[i]);
            }
            var indexMap = c1.ToList();
            var remap = new System.Collections.Generic.Dictionary<int, int>();
            for (int i = 0; i < indexMap.Count; i++)
            {
                int i1 = indexMap[i];
                remap[i1] = i;
            }
            int[] index = new int[modifiedIndex.Count];
            for (int i = 0; i < modifiedIndex.Count; i++)
            {
                int si = modifiedIndex[i];
                index[i] = remap[si];
            }

            Vector3[] positions = new Vector3[indexMap.Count];
            Vector3[] normals = new Vector3[indexMap.Count];
            Vector2[] uvs = new Vector2[indexMap.Count];
            ushort[] bones = new ushort[indexMap.Count * 4];
            Vector4[] weights = new Vector4[indexMap.Count];

            for (int i = 0; i < indexMap.Count; i++)
            {
                int i2 = indexMap[i];

                ref var vertex = ref pmx.Vertices[i2];

                positions[i] = Vector3.Transform(vertex.Coordinate, exportTransform);
                normals[i] = Vector3.Normalize(Vector3.TransformNormal(vertex.Normal, exportTransform));
                uvs[i] = vertex.UvCoordinate;
                bones[i * 4 + 0] = vertex.boneId0 >= 0 ? (ushort)vertex.boneId0 : (ushort)0;
                bones[i * 4 + 1] = vertex.boneId1 >= 0 ? (ushort)vertex.boneId1 : (ushort)0;
                bones[i * 4 + 2] = vertex.boneId2 >= 0 ? (ushort)vertex.boneId2 : (ushort)0;
                bones[i * 4 + 3] = vertex.boneId3 >= 0 ? (ushort)vertex.boneId3 : (ushort)0;
                weights[i].X = vertex.Weights.X;
                weights[i].Y = vertex.Weights.Y;
                weights[i].Z = vertex.Weights.Z;
                weights[i].W = vertex.Weights.W;
            }
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] >= pmx.Bones.Count || bones[i] < 0)
                {
                    bones[i] = 0;
                }
            }

            Dictionary<string, GLTFAccessor>[] targets = null;
            if (useMorph)
            {
                targets = new Dictionary<string, GLTFAccessor>[vertexMorphs.Length];
                int a1 = 0;
                for (int i = 0; i < vertexMorphs.Length; i++)
                {
                    PMX_Morph morph = vertexMorphs[i];

                    Vector3[] morphPosition = new Vector3[indexMap.Count];
                    foreach (var morphVertex in morph.MorphVertice)
                    {
                        if (remap.TryGetValue(morphVertex.VertexIndex, out int i1))
                            morphPosition[i1] = Vector3.Transform(morphVertex.Offset, exportTransform);
                    }

                    targets[i] = new Dictionary<string, GLTFAccessor>()
                    {
                        {"POSITION", GLTFWriterContext.CreateAccessor(null, morphPosition)}
                    };
                    a1++;
                }
            }

            for (int i = 0; i < index.Length; i += 3)
            {
                var temp = index[i + 2];
                index[i + 2] = index[i + 1];
                index[i + 1] = temp;
            }

            GLTFPrimitive primitive = new GLTFPrimitive();
            primitive.material = mat2mat[material];

            primitive.attributes = new Dictionary<string, GLTFAccessor>();
            primitive.attributes["POSITION"] = GLTFWriterContext.CreateAccessor(null, positions);
            primitive.attributes["NORMAL"] = GLTFWriterContext.CreateAccessor(null, normals);
            primitive.attributes["TEXCOORD_0"] = GLTFWriterContext.CreateAccessor(null, uvs);
            primitive.attributes["JOINTS_0"] = GLTFWriterContext.CreateAccessor4(null, bones, 34962);
            primitive.attributes["WEIGHTS_0"] = GLTFWriterContext.CreateAccessor(null, weights);
            primitive.indices = GLTFWriterContext.CreateAccessor(null, index, 34963);
            if (targets != null)
                primitive.targets = targets.ToArray();

            return primitive;
        }
    }
}

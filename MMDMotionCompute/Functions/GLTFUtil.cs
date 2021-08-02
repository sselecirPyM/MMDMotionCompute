using MMDMotionCompute.FileFormat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using MMDMotionCompute.GLTF;
using System.Runtime.InteropServices;
using System.Numerics;
using MMDMotionCompute.Utility;

namespace MMDMotionCompute.Functions
{
    public static class GLTFUtil
    {
        const int vertexStride = 56;
        public static void SaveAsGLTF2(PMXFormat pmx, VMDFormat vmd, string imagePath, string path)
        {
            GLTFWriterContext glTFWriterContext = new GLTFWriterContext();

            Dictionary<string, int> name2Bone = new Dictionary<string, int>();
            for (int i = 0; i < pmx.Bones.Count; i++)
            {
                PMX_Bone bone = pmx.Bones[i];
                name2Bone[bone.Name] = i;
            }

            string bufferFileName = Path.ChangeExtension(path, ".bin");
            Stream bufferStream = new FileStream(bufferFileName, FileMode.Create, FileAccess.Write, FileShare.None);
            BinaryWriterPlus binaryWriter = new BinaryWriterPlus(bufferStream);
            //var positons = pmx.Vertices.Select(u => u.Coordinate).ToArray();
            //var normals = pmx.Vertices.Select(u => u.Normal).ToArray();
            //var uvs = pmx.Vertices.Select(u => u.UvCoordinate).ToArray();
            WriteVertex(bufferStream, pmx.Vertices);

            int vertexBufferSize = (int)bufferStream.Position;
            int spos0 = (int)bufferStream.Position;
            for (int i = 0; i < pmx.TriangleIndexs.Length; i++)
            {
                binaryWriter.Write(pmx.TriangleIndexs[i]);
            }
            int indexBufferSize = (int)bufferStream.Position - spos0;
            int spos1 = (int)bufferStream.Position;

            for (int i = 0; i < pmx.Bones.Count; i++)
            {
                Matrix4x4 mat = Matrix4x4.CreateTranslation(-pmx.Bones[i].Position);
                binaryWriter.Write(mat);
            }
            int inverseMatrixSize = (int)bufferStream.Position - spos1;
            int spos2 = (int)bufferStream.Position;

            var vertMorphs = pmx.Morphs.Where(u => u.MorphVertexs != null).ToArray();
            for (int i = 0; i < vertMorphs.Length; i++)
            {
                //Vector3[] positons1 = positons.ToArray();
                Vector3[] positons1 = new Vector3[pmx.Vertices.Length];
                var morph = vertMorphs[i].MorphVertexs;
                for (int j = 0; j < morph.Length; j++)
                {
                    positons1[morph[j].VertexIndex] = morph[j].Offset;
                }
                bufferStream.Write(MemoryMarshal.Cast<Vector3, byte>(positons1));
            }
            int spos3 = (int)bufferStream.Position;
            int morphSize = (int)bufferStream.Position - spos2;

            if (vmd != null)
            {
                foreach (var keyFrames in vmd.BoneKeyFrameSet)
                {
                    var offset = (int)bufferStream.Position;
                    Vector3 relatePosition = Vector3.Zero;

                    if (name2Bone.TryGetValue(keyFrames.Key, out int boneIndex))
                    {
                        var bone = pmx.Bones[boneIndex];
                        relatePosition = bone.Position;

                        if (bone.ParentIndex >= 0 && bone.ParentIndex < pmx.Bones.Count)
                        {
                            relatePosition -= pmx.Bones[bone.ParentIndex].Position;
                        }
                    }

                    glTFWriterContext.bufferOffset[keyFrames.Key] = offset;
                    foreach (var keyFrame in keyFrames.Value)
                        binaryWriter.Write(keyFrame.Frame / 30.0f);
                    foreach (var keyFrame in keyFrames.Value)
                        binaryWriter.Write(keyFrame.rotation);
                    foreach (var keyFrame in keyFrames.Value)
                    {
                        binaryWriter.Write(keyFrame.Translation + relatePosition);
                    }
                    glTFWriterContext.bufferLength[keyFrames.Key] = (int)bufferStream.Position - offset;
                }
            }
            int spos4 = (int)bufferStream.Position;
            int animSize = (int)bufferStream.Position - spos3;

            bufferStream.Flush();
            bufferStream.Dispose();

            GLTFModel gltfModel = new GLTFModel();
            const int boneStartNode = 2;

            gltfModel.nodes = new GLTFNode[pmx.Bones.Count + boneStartNode];
            gltfModel.nodes[0] = new GLTFNode { name = pmx.Name, children = new[] { 1, 2 } };
            gltfModel.nodes[1] = new GLTFNode { name = pmx.Name, mesh = 0, skin = 0 };

            List<int>[] boneChilderens = new List<int>[pmx.Bones.Count];
            for (int i = 0; i < pmx.Bones.Count; i++)
            {
                PMX_Bone bone = pmx.Bones[i];
                if (bone.ParentIndex >= 0 && bone.ParentIndex < pmx.Bones.Count)
                {
                    if (boneChilderens[bone.ParentIndex] == null)
                        boneChilderens[bone.ParentIndex] = new List<int>();
                    boneChilderens[bone.ParentIndex].Add(i + boneStartNode);
                }
            }
            for (int i = 0; i < pmx.Bones.Count; i++)
            {
                var node = new GLTFNode { name = pmx.Bones[i].Name };
                PMX_Bone bone = pmx.Bones[i];
                gltfModel.nodes[i + boneStartNode] = node;
                if (boneChilderens[i] != null)
                    node.children = boneChilderens[i].ToArray();
                Vector3 relatePosition = bone.Position;
                if (bone.ParentIndex >= 0 && bone.ParentIndex < pmx.Bones.Count)
                {
                    relatePosition -= pmx.Bones[bone.ParentIndex].Position;
                }
                node.translation = new float[3] { relatePosition.X, relatePosition.Y, relatePosition.Z, };
            }
            {
                var skin = new GLTFSkin();
                List<int> joints = new List<int>();
                for (int i = boneStartNode; i < gltfModel.nodes.Length; i++)
                {
                    joints.Add(i);
                }
                skin.joints = joints.ToArray();
                skin.inverseBindMatrices = 5;
                skin.skeleton = boneStartNode;
                gltfModel.skins = new GLTFSkin[] { skin };
            }

            gltfModel.scenes = new[] { new GLTFScene { nodes = new[] { 0 } } };
            gltfModel.images = new GLTFImage[pmx.Textures.Count];
            gltfModel.textures = new GLTFTexture[pmx.Textures.Count];
            gltfModel.materials = new GLTFMaterial[pmx.Materials.Count];

            for (int i = 0; i < pmx.Textures.Count; i++)
                gltfModel.images[i] = new GLTFImage { uri = pmx.Textures[i].TexturePath };
            for (int i = 0; i < pmx.Textures.Count; i++)
                gltfModel.textures[i] = new GLTFTexture { source = i };
            for (int i = 0; i < pmx.Materials.Count; i++)
            {
                gltfModel.materials[i] = new GLTFMaterial();
                gltfModel.materials[i].name = pmx.Materials[i].Name;
                gltfModel.materials[i].pbrMetallicRoughness = new GLTFPBRMetallicRoughness { metallicFactor = 0.0f, roughnessFactor = 0.0f };
                if (pmx.Materials[i].TextureIndex >= 0)
                    gltfModel.materials[i].pbrMetallicRoughness.baseColorTexture = new GLTFTextureInfo { index = pmx.Materials[i].TextureIndex };
            }
            gltfModel.buffers = new[] { new GLTFBuffer { uri = Path.GetFileName(bufferFileName), byteLength = vertexBufferSize } };
            List<GLTFBufferView> bufferViews = new List<GLTFBufferView>
            {
                new GLTFBufferView{buffer=0, byteLength=vertexBufferSize, byteStride=vertexStride, target=34962},
                new GLTFBufferView{buffer=0, byteLength=indexBufferSize, byteOffset=spos0, target=34963},
                new GLTFBufferView{buffer=0, byteLength=inverseMatrixSize, byteOffset=spos1, target=34963},
                new GLTFBufferView{buffer=0, byteLength=morphSize, byteOffset=spos2, target=34963},
            };
            if (vmd != null)
            {
                bufferViews.Add(new GLTFBufferView { buffer = 0, byteLength = animSize, byteOffset = spos3, target = 34963 });
            }

            gltfModel.bufferViews = bufferViews.ToArray();
            var mesh = new GLTFMesh { name = pmx.Name };
            mesh.primitives = new GLTFPrimitive[pmx.Materials.Count];

            gltfModel.meshes = new GLTFMesh[] { mesh };
            glTFWriterContext.accessors.Add(new GLTFAccessor { bufferView = 0, componentType = 5126, byteOffset = 0, count = pmx.Vertices.Length, type = "VEC3" });
            glTFWriterContext.accessors.Add(new GLTFAccessor { bufferView = 0, componentType = 5126, byteOffset = 12, count = pmx.Vertices.Length, type = "VEC3" });
            glTFWriterContext.accessors.Add(new GLTFAccessor { bufferView = 0, componentType = 5126, byteOffset = 24, count = pmx.Vertices.Length, type = "VEC2" });
            glTFWriterContext.accessors.Add(new GLTFAccessor { bufferView = 0, componentType = 5123, byteOffset = 32, count = pmx.Vertices.Length, type = "VEC4" });
            glTFWriterContext.accessors.Add(new GLTFAccessor { bufferView = 0, componentType = 5126, byteOffset = 40, count = pmx.Vertices.Length, type = "VEC4" });
            glTFWriterContext.accessors.Add(new GLTFAccessor { bufferView = 2, componentType = 5126, byteOffset = 0, count = pmx.Bones.Count, type = "MAT4" });
            glTFWriterContext.StartWriteAccessorMark("material");

            for (int i = 0; i < pmx.Materials.Count; i++)
            {
                var material = pmx.Materials[i];
                glTFWriterContext.accessors.Add(new GLTFAccessor { bufferView = 1, byteOffset = material.TriangeIndexStartNum * 4, count = material.TriangeIndexNum, componentType = 5125, type = "SCALAR", });
            }

            glTFWriterContext.StartWriteAccessorMark("morph");
            for (int i = 0; i < vertMorphs.Length; i++)
            {
                glTFWriterContext.accessors.Add(new GLTFAccessor { bufferView = 3, componentType = 5126, byteOffset = i * pmx.Vertices.Length * 12, count = pmx.Vertices.Length, type = "VEC3" });
            }
            glTFWriterContext.StartWriteAccessorMark("animation");

            if (vmd != null)
            {
                foreach (var keyFrames in vmd.BoneKeyFrameSet)
                {
                    int bufOfs1 = glTFWriterContext.bufferOffset[keyFrames.Key] - spos3;

                    glTFWriterContext.StartWriteAccessorMark(keyFrames.Key);
                    glTFWriterContext.accessors.Add(new GLTFAccessor { bufferView = 4, componentType = 5126, byteOffset = bufOfs1, count = keyFrames.Value.Count, type = "SCALAR" });
                    glTFWriterContext.accessors.Add(new GLTFAccessor { bufferView = 4, componentType = 5126, byteOffset = bufOfs1 + keyFrames.Value.Count * 4, count = keyFrames.Value.Count, type = "VEC4" });
                    glTFWriterContext.accessors.Add(new GLTFAccessor { bufferView = 4, componentType = 5126, byteOffset = bufOfs1 + keyFrames.Value.Count * 20, count = keyFrames.Value.Count, type = "VEC3" });
                }
                List<GLTFAnimation> animations = new List<GLTFAnimation>();
                GLTFAnimation animation = new GLTFAnimation();
                animation.name = vmd.Name;
                List<GLTFAnimationSampler> samplers = new List<GLTFAnimationSampler>();
                List<GLTFAnimationChannel> channels = new List<GLTFAnimationChannel>();
                foreach (var keyFrames in vmd.BoneKeyFrameSet)
                {
                    if (name2Bone.TryGetValue(keyFrames.Key, out int index))
                    {
                        {

                            GLTFAnimationSampler samplerRot = new GLTFAnimationSampler();
                            samplerRot.input = glTFWriterContext.accessorStart[keyFrames.Key];
                            samplerRot.output = samplerRot.input + 1;
                            GLTFAnimationChannel channelRot = new GLTFAnimationChannel();
                            channelRot.sampler = samplers.Count;
                            channelRot.target = new GLTFAnimationChannelTarget
                            {
                                node = name2Bone[keyFrames.Key] + boneStartNode,
                                path = "rotation",
                            };
                            samplers.Add(samplerRot);
                            channels.Add(channelRot);
                        }
                        {

                            GLTFAnimationSampler samplerTran = new GLTFAnimationSampler();
                            samplerTran.input = glTFWriterContext.accessorStart[keyFrames.Key];
                            samplerTran.output = samplerTran.input + 2;
                            GLTFAnimationChannel channelTran = new GLTFAnimationChannel();
                            channelTran.sampler = samplers.Count;
                            channelTran.target = new GLTFAnimationChannelTarget
                            {
                                node = name2Bone[keyFrames.Key] + boneStartNode,
                                path = "translation",
                            };
                            samplers.Add(samplerTran);
                            channels.Add(channelTran);
                        }
                    }
                }
                animation.channels = channels.ToArray();
                animation.samplers = samplers.ToArray();
                animations.Add(animation);
                gltfModel.animations = animations.ToArray();
            }
            gltfModel.accessors = glTFWriterContext.accessors.ToArray();

            int startMaterial = glTFWriterContext.accessorStart["material"];
            int startMorph = glTFWriterContext.accessorStart["morph"];
            for (int i = 0; i < pmx.Materials.Count; i++)
            {
                Dictionary<string, int> attrs = new Dictionary<string, int>();
                attrs["POSITION"] = 0;
                attrs["NORMAL"] = 1;
                attrs["TEXCOORD_0"] = 2;
                attrs["JOINTS_0"] = 3;
                attrs["WEIGHTS_0"] = 4;

                mesh.primitives[i] = new GLTFPrimitive { material = i, indices = i + startMaterial, attributes = attrs };
                List<Dictionary<string, int>> targets = new List<Dictionary<string, int>>();
                for (int j = 0; j < vertMorphs.Length; j++)
                {
                    var target = new Dictionary<string, int>();
                    target["POSITION"] = startMorph + j;
                    targets.Add(target);
                }
                mesh.primitives[i].targets = targets.ToArray();
            }

            var option = new JsonSerializerOptions(JsonSerializerDefaults.General);
            option.WriteIndented = true;
            option.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            byte[] data = JsonSerializer.SerializeToUtf8Bytes(gltfModel, option);
            File.WriteAllBytes(path, data);

        }
        struct _Vertex
        {
            public Vector3 position;
            public Vector3 normal;
            public Vector2 uv;
            public short boneId0;
            public short boneId1;
            public short boneId2;
            public short boneId3;
            public Vector4 weight;
        }
        static BufferBytes WriteVertex(Stream stream, PMX_Vertex[] vertex)
        {
            int offset = (int)stream.Position;
            byte[] buffer1 = new byte[vertexStride];
            for (int i = 0; i < vertex.Length; i++)
            {
                _Vertex vertex1 = new _Vertex()
                {
                    position = vertex[i].Coordinate,
                    normal = vertex[i].Normal,
                    uv = vertex[i].UvCoordinate,
                    boneId0 = (vertex[i].boneId0 >= 0) ? (short)vertex[i].boneId0 : (short)0,
                    boneId1 = (vertex[i].boneId1 >= 0) ? (short)vertex[i].boneId1 : (short)0,
                    boneId2 = (vertex[i].boneId2 >= 0) ? (short)vertex[i].boneId2 : (short)0,
                    boneId3 = (vertex[i].boneId3 >= 0) ? (short)vertex[i].boneId3 : (short)0,
                    weight = vertex[i].Weights,
                };
                MemoryMarshal.Write(buffer1, ref vertex1);
                stream.Write(buffer1, 0, buffer1.Length);
            }
            return new BufferBytes() { offset = offset, size = (int)stream.Position - offset, };
        }

        //static BufferBytes WriteVectors(IEnumerable<Vector3> vectors, Stream stream)
        //{
        //    int offset = (int)stream.Position;
        //    float[] min = new float[3];
        //    float[] max = new float[3];

        //    byte[] buffer1 = new byte[16];

        //    const int sizeofVec3 = 12;

        //    {
        //        var vector1 = vectors.First();
        //        min[0] = Math.Min(vector1.X, min[0]);
        //        min[1] = Math.Min(vector1.Y, min[1]);
        //        min[2] = Math.Min(vector1.Z, min[2]);
        //        max[0] = Math.Max(vector1.X, max[0]);
        //        max[1] = Math.Max(vector1.Y, max[1]);
        //        max[2] = Math.Max(vector1.Z, max[2]);
        //    }
        //    foreach (var vector in vectors)
        //    {
        //        var vector1 = vector;
        //        min[0] = Math.Min(vector1.X, min[0]);
        //        min[1] = Math.Min(vector1.Y, min[1]);
        //        min[2] = Math.Min(vector1.Z, min[2]);
        //        max[0] = Math.Max(vector1.X, max[0]);
        //        max[1] = Math.Max(vector1.Y, max[1]);
        //        max[2] = Math.Max(vector1.Z, max[2]);
        //        MemoryMarshal.Write(buffer1, ref vector1);
        //        stream.Write(buffer1, 0, sizeofVec3);
        //    }

        //    return new BufferBytes() { offset = offset, size = (int)stream.Position - offset, min = min, max = max, };
        //}

        //static BufferBytes WriteVectors(IEnumerable<Vector2> vectors, Stream stream)
        //{
        //    int offset = (int)stream.Position;
        //    float[] min = new float[2];
        //    float[] max = new float[2];

        //    byte[] buffer1 = new byte[16];

        //    const int sizeofVec3 = 8;

        //    {
        //        var vector1 = vectors.First();
        //        min[0] = Math.Min(vector1.X, min[0]);
        //        min[1] = Math.Min(vector1.Y, min[1]);
        //        max[0] = Math.Max(vector1.X, max[0]);
        //        max[1] = Math.Max(vector1.Y, max[1]);
        //    }
        //    foreach (var vector in vectors)
        //    {
        //        var vector1 = vector;
        //        min[0] = Math.Min(vector1.X, min[0]);
        //        min[1] = Math.Min(vector1.Y, min[1]);
        //        max[0] = Math.Max(vector1.X, max[0]);
        //        max[1] = Math.Max(vector1.Y, max[1]);
        //        MemoryMarshal.Write(buffer1, ref vector1);
        //        stream.Write(buffer1, 0, sizeofVec3);
        //    }

        //    return new BufferBytes() { offset = offset, size = (int)stream.Position - offset, min = min, max = max, };
        //}

        struct BufferBytes
        {
            public int offset;
            public int size;
            public int end { get => offset + size; }
            public float[] min;
            public float[] max;
        }
    }
}

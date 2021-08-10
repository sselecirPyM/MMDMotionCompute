using MMDMotionCompute.MMD;
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
using MMDMotionCompute.Physics;

namespace MMDMotionCompute.Functions
{
    public static class GLTFUtil
    {
        const int vertexStride = 56;
        public static void SaveAsGLTF2(PMXFormat pmx, VMDFormat vmd, ExportOptions options, string path)
        {
            PMXOptimizer1 optimizer1 = new PMXOptimizer1();
            //pmx = optimizer1.OptimizePMX(pmx);

            GLTFWriterContext glTFWriterContext = new GLTFWriterContext();
            var vertMorphs = pmx.Morphs.Where(u => u.MorphVertexs != null).ToArray();

            var vertMorphsIndices = new List<HashSet<int>>();
            foreach (var p in vertMorphs)
            {
                vertMorphsIndices.Add(p.MorphVertexs.Select(u => u.VertexIndex).ToHashSet());
            }

            Matrix4x4 exportTransform = Matrix4x4.CreateScale(-1, 1, 1) * Matrix4x4.CreateScale(0.08f);

            Dictionary<string, int> name2Bone = GetName2Index(pmx.Bones, u => u.Name);
            Dictionary<string, int> name2Morph = GetName2Index(vertMorphs, u => u.Name);

            string bufferFileName = Path.ChangeExtension(path, ".bin");
            Stream bufferStream = new FileStream(bufferFileName, FileMode.Create, FileAccess.Write, FileShare.None);
            BinaryWriterPlus writer = new BinaryWriterPlus(bufferStream);
            glTFWriterContext.writer = writer;
            var accessors = glTFWriterContext.accessors;
            if (true)
            {
                float[] min;
                float[] max;
                WriteVectors(pmx.Vertices.Select(u => Vector3.Transform(u.Coordinate, exportTransform)).ToArray(), writer, out min, out max);
                glTFWriterContext.MarkBuf("_position", new GLTFBufferView { target = 34962 }, new GLTFAccessor { componentType = 5126, count = pmx.Vertices.Length, type = "VEC3", min = min, max = max });
                WriteVectors(pmx.Vertices.Select(u => InvertX(u.Normal)).ToArray(), writer, out min, out max);
                glTFWriterContext.MarkBuf("_normal", new GLTFBufferView { target = 34962 }, new GLTFAccessor { componentType = 5126, count = pmx.Vertices.Length, type = "VEC3", min = min, max = max });
                WriteVectors(pmx.Vertices.Select(u => u.UvCoordinate).ToArray(), writer, out min, out max);
                glTFWriterContext.MarkBuf("_uv", new GLTFBufferView { target = 34962 }, new GLTFAccessor { componentType = 5126, count = pmx.Vertices.Length, type = "VEC2", min = min, max = max });

                var vertex = pmx.Vertices;
                for (int i = 0; i < vertex.Length; i++)
                {
                    writer.Write((vertex[i].boneId0 >= 0) ? (short)vertex[i].boneId0 : (short)0);
                    writer.Write((vertex[i].boneId1 >= 0) ? (short)vertex[i].boneId1 : (short)0);
                    writer.Write((vertex[i].boneId2 >= 0) ? (short)vertex[i].boneId2 : (short)0);
                    writer.Write((vertex[i].boneId3 >= 0) ? (short)vertex[i].boneId3 : (short)0);
                }
                glTFWriterContext.MarkBuf("_joints", new GLTFBufferView { }, new GLTFAccessor { componentType = 5123, count = pmx.Vertices.Length, type = "VEC4" });

                writer.Write(MemoryMarshal.AsBytes<Vector4>(pmx.Vertices.Select(u => u.Weights).ToArray()));
                glTFWriterContext.MarkBuf("_weights", new GLTFBufferView { }, new GLTFAccessor { componentType = 5126, count = pmx.Vertices.Length, type = "VEC4" });
            }
            //else
            //{
            //    WriteVertex(writer, pmx.Vertices, exportTransform);

            //    glTFWriterContext.MarkBuf("_vertex", new GLTFBufferView { byteStride = vertexStride, target = 34962 }, null);
            //    accessors.Add(new GLTFAccessor { bufferView = 0, componentType = 5126, byteOffset = 0, count = pmx.Vertices.Length, type = "VEC3" });
            //    accessors.Add(new GLTFAccessor { bufferView = 0, componentType = 5126, byteOffset = 12, count = pmx.Vertices.Length, type = "VEC3" });
            //    accessors.Add(new GLTFAccessor { bufferView = 0, componentType = 5126, byteOffset = 24, count = pmx.Vertices.Length, type = "VEC2" });
            //    accessors.Add(new GLTFAccessor { bufferView = 0, componentType = 5123, byteOffset = 32, count = pmx.Vertices.Length, type = "VEC4" });
            //    accessors.Add(new GLTFAccessor { bufferView = 0, componentType = 5126, byteOffset = 40, count = pmx.Vertices.Length, type = "VEC4" });
            //}

            int[] indexArray = pmx.TriangleIndexs.ToArray();

            TMaterial[] material1 = new TMaterial[pmx.Materials.Count];

            for (int i = 0; i < pmx.Materials.Count; i++)
            {
                var mat = pmx.Materials[i];
                Range range = new Range(mat.TriangeIndexStartNum, mat.TriangeIndexNum + mat.TriangeIndexStartNum);
                material1[i] = new TMaterial();
                material1[i].startVertex = indexArray[range].Min();
                int endVertex = indexArray[range].Max();
                material1[i].VertexCount = endVertex - material1[i].startVertex;
                material1[i].Name = mat.Name;
                material1[i].startIndex = mat.TriangeIndexStartNum;
                material1[i].indexCount = mat.TriangeIndexNum;
            }
            var s1 = material1.ToList();
            //s1.Sort((x, y) => { return x.startIndex.CompareTo(y.startIndex); });
            bool vertOverlap = false;
            for (int i = 0; i < s1.Count - 1; i++)
            {
                if (s1[i].startVertex + s1[i].VertexCount > s1[i + 1].startVertex)
                {
                    vertOverlap = true;
                }
            }


            for (int i = 0; i < indexArray.Length - 2; i += 3)
            {
                int t1 = indexArray[i + 1];
                int t2 = indexArray[i + 2];
                indexArray[i + 1] = t2;
                indexArray[i + 2] = t1;
            }
            writer.Write(MemoryMarshal.AsBytes<int>(indexArray));
            glTFWriterContext.MarkBuf("_index", new GLTFBufferView { target = 34963 }, null);
            glTFWriterContext.StartWriteAccessorMark("material");
            for (int i = 0; i < pmx.Materials.Count; i++)
            {
                var material = pmx.Materials[i];
                accessors.Add(new GLTFAccessor { bufferView = glTFWriterContext.bufferViewStart["_index"], byteOffset = material.TriangeIndexStartNum * 4, count = material.TriangeIndexNum, componentType = 5125, type = "SCALAR", });
            }

            for (int i = 0; i < pmx.Bones.Count; i++)
            {
                writer.Write(Matrix4x4.CreateTranslation(Vector3.Transform(-pmx.Bones[i].Position, exportTransform)));
            }
            glTFWriterContext.MarkBuf("_inverseBindMatrices", new GLTFBufferView { }, new GLTFAccessor { componentType = 5126, count = pmx.Bones.Count, type = "MAT4" });

            glTFWriterContext.StartWriteAccessorMark("morph");
            for (int i = 0; i < vertMorphs.Length; i++)
            {
                var morph = vertMorphs[i].MorphVertexs;
                if (options.sparseMorph)
                {
                    int[] indices = new int[morph.Length];
                    for (int j = 0; j < morph.Length; j++)
                    {
                        indices[j] = morph[j].VertexIndex;
                    }
                    bufferStream.Write(MemoryMarshal.Cast<int, byte>(indices));
                    glTFWriterContext.MarkBuf(vertMorphs[i].Name + "/indices", new GLTFBufferView { }, null);

                    WriteVectors(morph.Select(u => Vector3.Transform(u.Offset, exportTransform)).ToArray(), writer, out float[] min, out float[] max);
                    glTFWriterContext.MarkBuf(vertMorphs[i].Name + "/position", new GLTFBufferView { }, null);
                    for (int k = 0; k < 3; k++)
                    {
                        min[k] = Math.Min(0, min[k]);
                        max[k] = Math.Max(0, max[k]);
                    }
                    glTFWriterContext.accessors.Add(new GLTFAccessor
                    {
                        sparse = new GLTFAccessorSparse
                        {
                            count = morph.Length,
                            indices = new GLTFAccessorSparseIndices { bufferView = glTFWriterContext.bufferViewStart[vertMorphs[i].Name + "/indices"], componentType = 5125 },
                            values = new GLTFAccessorSparseValues { bufferView = glTFWriterContext.bufferViewStart[vertMorphs[i].Name + "/position"] }
                        },
                        count = pmx.Vertices.Length,
                        componentType = 5126,
                        type = "VEC3",
                        min = min,
                        max = max,
                    });

                }
                else
                {
                    Vector3[] positons1 = new Vector3[pmx.Vertices.Length];
                    for (int j = 0; j < morph.Length; j++)
                    {
                        positons1[morph[j].VertexIndex] = Vector3.Transform(morph[j].Offset, exportTransform);
                    }
                    WriteVectors(positons1, writer, out float[] min, out float[] max);
                    glTFWriterContext.MarkBuf(vertMorphs[i].Name, new GLTFBufferView { target = 34963 }, new GLTFAccessor { componentType = 5126, count = pmx.Vertices.Length, type = "VEC3", min = min, max = max });
                }

            }
            MMDCharacter character;

            Dictionary<string, List<BoneKeyFrame>> boneKeyFrames = new Dictionary<string, List<BoneKeyFrame>>();
            if (vmd != null)
            {
                character = MMDCharacter.Load(pmx);
                MMDMotion motion = VMDFormatExtension.Load(vmd);
                PhysicsScene scene = new PhysicsScene();
                if (options.physics)
                {
                    scene.Initialize();
                    scene.SetGravitation(new Vector3(0, -options.gravity, 0));
                    character.AddPhysics(scene);
                }
                foreach (var bone in character.bones)
                {
                    boneKeyFrames[bone.Name] = new List<BoneKeyFrame>();
                }
                int c_sampleCount = 2;
                for (int t = 0; t < motion.lastFrame * c_sampleCount; t++)
                {
                    float time = t / 30.0f / c_sampleCount;
                    if (options.physics)
                    {
                        if (t == 0)
                        {
                            void _reset()
                            {
                                character.SetMotionTime(time, motion);
                                character.PrePhysicsSync(scene);
                                character.ResetPhysics(scene);
                                scene.Simulation(1 / 60.0);
                            }
                            _reset();
                            _reset();
                            _reset();
                            _reset();
                        }
                        character.SetMotionTime(time - 1 / 30.0f / c_sampleCount, motion);
                        character.PrePhysicsSync(scene);
                        scene.Simulation(1 / 30.0 / c_sampleCount);
                        character.PhysicsSync(scene);
                    }
                    else
                    {
                        character.SetMotionTime(time, motion);
                    }

                    foreach (var bone in character.bones)
                    {
                        boneKeyFrames[bone.Name].Add(new BoneKeyFrame { Frame = t, Translation = bone.dynamicPosition, Rotation = bone.finalRotation });
                    }
                }
                float[] min;
                float[] max;
                foreach (var keyFrames in boneKeyFrames)
                {
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

                    bool hasRots = true;
                    bool hasTrans = true;

                    foreach (var keyFrame in keyFrames.Value)
                        if (keyFrame.rotation != Quaternion.Identity)
                        {
                            hasRots = true;
                            break;
                        }
                    foreach (var keyFrame in keyFrames.Value)
                        if (keyFrame.Translation != Vector3.Zero)
                        {
                            hasTrans = true;
                            break;
                        }
                    if (hasRots || hasTrans)
                    {
                        WriteFloats(keyFrames.Value.Select(u => u.Frame / 30.0f / c_sampleCount).ToArray(), writer, out min, out max);
                        glTFWriterContext.MarkBuf(keyFrames.Key + "/_keyFrameTime", new GLTFBufferView { }, new GLTFAccessor { componentType = 5126, count = keyFrames.Value.Count, type = "SCALAR", min = min, max = max });
                    }
                    if (hasRots)
                    {
                        foreach (var keyFrame in keyFrames.Value)
                            writer.Write(new Quaternion(keyFrame.rotation.X, -keyFrame.rotation.Y, -keyFrame.rotation.Z, keyFrame.rotation.W));
                        glTFWriterContext.MarkBuf(keyFrames.Key + "/_keyFrameRotation", new GLTFBufferView { }, new GLTFAccessor { componentType = 5126, count = keyFrames.Value.Count, type = "VEC4" });
                    }
                    if (hasTrans)
                    {
                        foreach (var keyFrame in keyFrames.Value)
                            writer.Write(Vector3.Transform(keyFrame.Translation + relatePosition, exportTransform));
                        glTFWriterContext.MarkBuf(keyFrames.Key + "/_keyFrameTranslation", new GLTFBufferView { }, new GLTFAccessor { componentType = 5126, count = keyFrames.Value.Count, type = "VEC3" });
                    }
                }


                List<float[]> keyFrames1 = VMDSampler1.ProcessVertMorph(vmd, vertMorphs, out float[] morphFrameTime);

                //for (int i = 0; i < morphFrameTime.Length; i++)
                //{
                //    writer.Write(morphFrameTime[i]);
                //}
                WriteFloats(morphFrameTime, writer, out min, out max);
                glTFWriterContext.MarkBuf("_keyFrameMorphTime", new GLTFBufferView { }, new GLTFAccessor { componentType = 5126, count = keyFrames1.Count, type = "SCALAR", min = min, max = max });
                for (int i = 0; i < keyFrames1.Count; i++)
                {
                    for (int j = 0; j < keyFrames1[i].Length; j++)
                        writer.Write(keyFrames1[i][j]);
                }
                glTFWriterContext.MarkBuf("_keyFrameMorphWeight", new GLTFBufferView { }, new GLTFAccessor { componentType = 5126, count = keyFrames1.Count * vertMorphs.Length, type = "SCALAR" });

            }
            int totalSize = (int)bufferStream.Position;
            bufferStream.Flush();
            bufferStream.Dispose();

            GLTFModel gltfModel = new GLTFModel();
            const int boneStartNode = 1;

            List<GLTFNode> nodes = new List<GLTFNode>();
            nodes.Add(new GLTFNode { name = pmx.Name, mesh = 0, skin = 0, children = new[] { 1 } });

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
                nodes.Add(node);
                //gltfModel.nodes[i + boneStartNode] = node;
                if (boneChilderens[i] != null)
                    node.children = boneChilderens[i].ToArray();
                Vector3 relatePosition = bone.Position;
                if (bone.ParentIndex >= 0 && bone.ParentIndex < pmx.Bones.Count)
                {
                    relatePosition -= pmx.Bones[bone.ParentIndex].Position;
                }
                relatePosition = Vector3.Transform(relatePosition, exportTransform);
                node.translation = new float[3] { relatePosition.X, relatePosition.Y, relatePosition.Z, };
            }
            gltfModel.nodes = nodes.ToArray();
            {
                var skin = new GLTFSkin();
                List<int> joints = new List<int>();
                for (int i = boneStartNode; i < gltfModel.nodes.Length; i++)
                {
                    joints.Add(i);
                }
                skin.joints = joints.ToArray();
                skin.inverseBindMatrices = glTFWriterContext.accessorStart["_inverseBindMatrices"];
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
                gltfModel.materials[i].doubleSided = pmx.Materials[i].DrawFlags.HasFlag(PMX_DrawFlag.DrawDoubleFace);
                if (pmx.Materials[i].TextureIndex >= 0)
                    gltfModel.materials[i].pbrMetallicRoughness.baseColorTexture = new GLTFTextureInfo { index = pmx.Materials[i].TextureIndex };
            }
            gltfModel.buffers = new[] { new GLTFBuffer { uri = Path.GetFileName(bufferFileName), byteLength = totalSize } };
            List<GLTFBufferView> bufferViews = glTFWriterContext.bufferViews;



            if (vmd != null)
            {
                List<GLTFAnimation> animations = new List<GLTFAnimation>();
                GLTFAnimation animation = new GLTFAnimation();
                animation.name = vmd.Name;
                List<GLTFAnimationSampler> samplers = new List<GLTFAnimationSampler>();
                List<GLTFAnimationChannel> channels = new List<GLTFAnimationChannel>();
                foreach (var keyFrames in boneKeyFrames)
                {
                    if (name2Bone.TryGetValue(keyFrames.Key, out int index))
                    {
                        if (glTFWriterContext.accessorStart.TryGetValue(keyFrames.Key + "/_keyFrameRotation", out int rotsIndex))
                        {
                            GLTFAnimationSampler samplerRot = new GLTFAnimationSampler();
                            samplerRot.input = glTFWriterContext.accessorStart[keyFrames.Key + "/_keyFrameTime"];
                            samplerRot.output = rotsIndex;
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
                        if (glTFWriterContext.accessorStart.TryGetValue(keyFrames.Key + "/_keyFrameTranslation", out int transIndex))
                        {
                            GLTFAnimationSampler samplerTran = new GLTFAnimationSampler();
                            samplerTran.input = glTFWriterContext.accessorStart[keyFrames.Key + "/_keyFrameTime"];
                            samplerTran.output = transIndex;
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

                {
                    GLTFAnimationSampler samplerMorph = new GLTFAnimationSampler();
                    samplerMorph.input = glTFWriterContext.accessorStart["_keyFrameMorphTime"];
                    samplerMorph.output = glTFWriterContext.accessorStart["_keyFrameMorphWeight"];
                    GLTFAnimationChannel channelRot = new GLTFAnimationChannel();
                    channelRot.sampler = samplers.Count;
                    channelRot.target = new GLTFAnimationChannelTarget
                    {
                        node = boneStartNode - 1,
                        path = "weights",
                    };
                    samplers.Add(samplerMorph);
                    channels.Add(channelRot);
                }

                animation.channels = channels.ToArray();
                animation.samplers = samplers.ToArray();
                animations.Add(animation);
                gltfModel.animations = animations.ToArray();
            }
            gltfModel.bufferViews = bufferViews.ToArray();
            gltfModel.accessors = accessors.ToArray();
            gltfModel.asset.generator = "MMDMotionCompute";

            int startMaterialAccessor = glTFWriterContext.accessorStart["material"];
            int startMorph = glTFWriterContext.accessorStart["morph"];

            var morphMesh = new GLTFMesh { name = pmx.Name };
            var noMorphMesh = new GLTFMesh { name = pmx.Name };

            List<GLTFPrimitive> morphPrimitives = new List<GLTFPrimitive>();
            List<GLTFPrimitive> noMorphPrimitives = new List<GLTFPrimitive>();
            for (int i = 0; i < pmx.Materials.Count; i++)
            {
                var mat = pmx.Materials[i];
                Dictionary<string, int> attrs = new Dictionary<string, int>();
                attrs["POSITION"] = glTFWriterContext.accessorStart["_position"];
                attrs["NORMAL"] = glTFWriterContext.accessorStart["_normal"];
                attrs["TEXCOORD_0"] = glTFWriterContext.accessorStart["_uv"];
                attrs["JOINTS_0"] = glTFWriterContext.accessorStart["_joints"];
                attrs["WEIGHTS_0"] = glTFWriterContext.accessorStart["_weights"];
                GLTFPrimitive primitive = new GLTFPrimitive { material = i, indices = i + startMaterialAccessor, attributes = attrs };
                morphPrimitives.Add(primitive);
                List<Dictionary<string, int>> targets = new List<Dictionary<string, int>>();
                bool anyMorph = false;
                for (int j = 0; j < vertMorphs.Length; j++)
                {
                    var target = new Dictionary<string, int>();
                    target["POSITION"] = startMorph + j;
                    Range range = new Range(mat.TriangeIndexStartNum, mat.TriangeIndexNum + mat.TriangeIndexStartNum);
                    //if (vertMorphsIndices[j].Overlaps(indexArray[range]))
                    //{
                    //    anyMorph = true;
                    //}
                    targets.Add(target);
                }
                //if (anyMorph)
                primitive.targets = targets.ToArray();
            }
            morphMesh.primitives = morphPrimitives.ToArray();
            noMorphMesh.primitives = noMorphPrimitives.ToArray();
            gltfModel.meshes = new GLTFMesh[] { morphMesh };

            var option = new JsonSerializerOptions(JsonSerializerDefaults.General);
            option.WriteIndented = true;
            option.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            byte[] data = JsonSerializer.SerializeToUtf8Bytes(gltfModel, option);
            File.WriteAllBytes(path, data);

        }

        static Dictionary<string, int> GetName2Index<TSource>(IReadOnlyList<TSource> sources, Func<TSource, string> getName)
        {
            Dictionary<string, int> dictionary = new Dictionary<string, int>();
            for (int i = 0; i < sources.Count; i++)
            {
                dictionary[getName(sources[i])] = i;
            }
            return dictionary;
        }

        static void WriteVertex(BinaryWriterPlus writer, PMX_Vertex[] vertex, Matrix4x4 exportTransform)
        {
            for (int i = 0; i < vertex.Length; i++)
            {
                writer.Write(Vector3.Transform(vertex[i].Coordinate, exportTransform));
                writer.Write(InvertX(vertex[i].Normal));
                writer.Write(vertex[i].UvCoordinate);
                writer.Write((vertex[i].boneId0 >= 0) ? (short)vertex[i].boneId0 : (short)0);
                writer.Write((vertex[i].boneId1 >= 0) ? (short)vertex[i].boneId1 : (short)0);
                writer.Write((vertex[i].boneId2 >= 0) ? (short)vertex[i].boneId2 : (short)0);
                writer.Write((vertex[i].boneId3 >= 0) ? (short)vertex[i].boneId3 : (short)0);
                writer.Write(vertex[i].Weights);
            }
        }

        static Vector3 InvertX(Vector3 vec3)
        {
            return new Vector3(-vec3.X, vec3.Y, vec3.Z);
        }

        static void WriteVectors(Vector3[] vec3s, BinaryWriterPlus writer, out float[] min, out float[] max)
        {
            min = new float[3];
            max = new float[3];

            min[0] = vec3s[0].X;
            min[1] = vec3s[0].Y;
            min[2] = vec3s[0].Z;
            max[0] = vec3s[0].X;
            max[1] = vec3s[0].Y;
            max[2] = vec3s[0].Z;

            for (int i = 0; i < vec3s.Length; i++)
            {
                min[0] = Math.Min(vec3s[i].X, min[0]);
                min[1] = Math.Min(vec3s[i].Y, min[1]);
                min[2] = Math.Min(vec3s[i].Z, min[2]);
                max[0] = Math.Max(vec3s[i].X, max[0]);
                max[1] = Math.Max(vec3s[i].Y, max[1]);
                max[2] = Math.Max(vec3s[i].Z, max[2]);
            }
            writer.Write(MemoryMarshal.AsBytes<Vector3>(vec3s));
        }

        static void WriteVectors(Vector2[] vec2s, BinaryWriterPlus writer, out float[] min, out float[] max)
        {
            min = new float[2];
            max = new float[2];

            min[0] = vec2s[0].X;
            min[1] = vec2s[0].Y;
            max[0] = vec2s[0].X;
            max[1] = vec2s[0].Y;

            for (int i = 0; i < vec2s.Length; i++)
            {
                min[0] = Math.Min(vec2s[i].X, min[0]);
                min[1] = Math.Min(vec2s[i].Y, min[1]);
                max[0] = Math.Max(vec2s[i].X, max[0]);
                max[1] = Math.Max(vec2s[i].Y, max[1]);
            }
            writer.Write(MemoryMarshal.AsBytes<Vector2>(vec2s));
        }

        static void WriteFloats(float[] vec2s, BinaryWriterPlus writer, out float[] min, out float[] max)
        {
            min = new float[1];
            max = new float[1];

            min[0] = vec2s[0];
            max[0] = vec2s[0];

            for (int i = 0; i < vec2s.Length; i++)
            {
                min[0] = Math.Min(vec2s[i], min[0]);
                max[0] = Math.Max(vec2s[i], max[0]);
            }
            writer.Write(MemoryMarshal.AsBytes<float>(vec2s));
        }
    }
}

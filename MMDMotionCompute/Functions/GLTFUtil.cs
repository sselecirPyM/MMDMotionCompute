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
            GLTFWriterContext glTFWriterContext = new GLTFWriterContext();
            var vertMorphs = pmx.Morphs.Where(u => u.MorphVertexs != null).ToArray();
            Matrix4x4 exportTransform = Matrix4x4.CreateScale(-1, 1, 1) * Matrix4x4.CreateScale(0.08f);

            Dictionary<string, int> name2Bone = new Dictionary<string, int>();
            for (int i = 0; i < pmx.Bones.Count; i++)
            {
                PMX_Bone bone = pmx.Bones[i];
                name2Bone[bone.Name] = i;
            }
            Dictionary<string, int> name2Morph = new Dictionary<string, int>();
            for (int i = 0; i < vertMorphs.Length; i++)
            {
                PMX_Morph morph = vertMorphs[i];
                name2Morph[morph.Name] = i;
            }

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
            else
            {
                WriteVertex(writer, pmx.Vertices, exportTransform);

                glTFWriterContext.MarkBuf("_vertex", new GLTFBufferView { byteStride = vertexStride, target = 34962 }, null);
                accessors.Add(new GLTFAccessor { bufferView = 0, componentType = 5126, byteOffset = 0, count = pmx.Vertices.Length, type = "VEC3" });
                accessors.Add(new GLTFAccessor { bufferView = 0, componentType = 5126, byteOffset = 12, count = pmx.Vertices.Length, type = "VEC3" });
                accessors.Add(new GLTFAccessor { bufferView = 0, componentType = 5126, byteOffset = 24, count = pmx.Vertices.Length, type = "VEC2" });
                accessors.Add(new GLTFAccessor { bufferView = 0, componentType = 5123, byteOffset = 32, count = pmx.Vertices.Length, type = "VEC4" });
                accessors.Add(new GLTFAccessor { bufferView = 0, componentType = 5126, byteOffset = 40, count = pmx.Vertices.Length, type = "VEC4" });
            }
            int[] indexArray = pmx.TriangleIndexs.ToArray();
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
                        //bufferView = 0,
                        //byteOffset = 0,
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
                        boneKeyFrames[bone.Name].Add(new BoneKeyFrame { Frame = t, Translation = bone.dynamicPosition, Rotation = bone.rotation });
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

                    //foreach (var keyFrame in keyFrames.Value)
                    //    writer.Write(keyFrame.Frame / 30.0f / c_sampleCount);
                   ;
                    WriteFloats(keyFrames.Value.Select(u => u.Frame / 30.0f / c_sampleCount).ToArray(), writer, out  min, out max);
                    glTFWriterContext.MarkBuf(keyFrames.Key + "/_keyFrameTime", new GLTFBufferView { }, new GLTFAccessor { componentType = 5126, count = keyFrames.Value.Count, type = "SCALAR", min = min, max = max });

                    foreach (var keyFrame in keyFrames.Value)
                    {
                        writer.Write(new Quaternion(keyFrame.rotation.X, -keyFrame.rotation.Y, -keyFrame.rotation.Z, keyFrame.rotation.W));
                    }
                    glTFWriterContext.MarkBuf(keyFrames.Key + "/_keyFrameRotation", new GLTFBufferView { }, new GLTFAccessor { componentType = 5126, count = keyFrames.Value.Count, type = "VEC4" });

                    foreach (var keyFrame in keyFrames.Value)
                        writer.Write(Vector3.Transform(keyFrame.Translation + relatePosition, exportTransform));
                    glTFWriterContext.MarkBuf(keyFrames.Key + "/_keyFrameTranslation", new GLTFBufferView { }, new GLTFAccessor { componentType = 5126, count = keyFrames.Value.Count, type = "VEC3" });
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

            gltfModel.nodes = new GLTFNode[pmx.Bones.Count + boneStartNode];
            gltfModel.nodes[0] = new GLTFNode { name = pmx.Name, mesh = 0, skin = 0, children = new[] { 1 } };

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
                relatePosition = Vector3.Transform(relatePosition, exportTransform);
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

            var mesh = new GLTFMesh { name = pmx.Name };
            mesh.primitives = new GLTFPrimitive[pmx.Materials.Count];

            gltfModel.meshes = new GLTFMesh[] { mesh };


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
                        {
                            GLTFAnimationSampler samplerRot = new GLTFAnimationSampler();
                            samplerRot.input = glTFWriterContext.accessorStart[keyFrames.Key + "/_keyFrameTime"];
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
                            samplerTran.input = glTFWriterContext.accessorStart[keyFrames.Key + "/_keyFrameTime"];
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

                {
                    GLTFAnimationSampler samplerMorph = new GLTFAnimationSampler();
                    samplerMorph.input = glTFWriterContext.accessorStart["_keyFrameMorphTime"];
                    samplerMorph.output = samplerMorph.input + 1;
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

            int startMaterialAccessor = glTFWriterContext.accessorStart["material"];
            int startMorph = glTFWriterContext.accessorStart["morph"];
            for (int i = 0; i < pmx.Materials.Count; i++)
            {
                Dictionary<string, int> attrs = new Dictionary<string, int>();
                attrs["POSITION"] = 0;
                attrs["NORMAL"] = 1;
                attrs["TEXCOORD_0"] = 2;
                attrs["JOINTS_0"] = 3;
                attrs["WEIGHTS_0"] = 4;

                mesh.primitives[i] = new GLTFPrimitive { material = i, indices = i + startMaterialAccessor, attributes = attrs };
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
            writer.Write(MemoryMarshal.Cast<Vector3, byte>(vec3s));
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
        }

        static void WriteVectors(Vector2[] vec2s, BinaryWriterPlus writer, out float[] min, out float[] max)
        {
            writer.Write(MemoryMarshal.Cast<Vector2, byte>(vec2s));
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
        }

        static void WriteFloats(float[] vec2s, BinaryWriterPlus writer, out float[] min, out float[] max)
        {
            writer.Write(MemoryMarshal.Cast<float, byte>(vec2s));
            min = new float[1];
            max = new float[1];

            min[0] = vec2s[0];
            max[0] = vec2s[0];

            for (int i = 0; i < vec2s.Length; i++)
            {
                min[0] = Math.Min(vec2s[i], min[0]);
                max[0] = Math.Max(vec2s[i], max[0]);
            }
        }
    }
}

using MMDMC.GLTF;
using MMDMC.MMD;
using MMDMC.Physics;
using MMDMC.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace MMDMC.Functions
{
    public static class GLTFUtil
    {
        const int vertexStride = 56;
        public static void SaveAsGLTF2(PMXFormat pmx, VMDFormat vmd, ExportOptions options, string path)
        {
            //PMXOptimizer1 optimizer1 = new PMXOptimizer1();
            //pmx = optimizer1.OptimizePMX(pmx);

            ModelContext modelContext = new ModelContext()
            {
                pmx = pmx,
            };
            modelContext.CollectMaterials();

            GLTFWriterContext glTFWriterContext = new GLTFWriterContext();
            modelContext.GLTFWriterContext = glTFWriterContext;
            var vertexMorphs = pmx.Morphs.Where(u => u.MorphVertice != null).ToArray();


            Matrix4x4 exportTransform = Matrix4x4.CreateScale(1, 1, 1) * Matrix4x4.CreateScale(options.exportScale);

            Dictionary<string, int> name2Bone = GetName2Index(pmx.Bones, u => u.Name);

            string bufferFileName = Path.ChangeExtension(path, ".bin");
            using Stream bufferStream = new FileStream(bufferFileName, FileMode.Create, FileAccess.Write, FileShare.None);
            BinaryWriterPlus writer = new BinaryWriterPlus(bufferStream);
            glTFWriterContext.writer = writer;


            GLTFModel gltfModel = new GLTFModel();


            var bones = modelContext.GetBones(exportTransform);
            var skin = modelContext.GetSkin(exportTransform, glTFWriterContext);
            List<GLTFNode> nodes = new List<GLTFNode>();
            for (int i = 0; i < 2; i++)
            {
                var node = new GLTFNode { name = pmx.Name + i };
                node.skin = skin;
                nodes.Add(node);
            }
            nodes[0].children = new List<GLTFNode>();
            nodes[0].children.Add(bones[0]);
            nodes.AddRange(bones);

            gltfModel.skins = new GLTFSkin[] { skin };


            gltfModel.nodes = nodes.ToArray();
            gltfModel.scenes = new[] { new GLTFScene { nodes = new[] { nodes[0], nodes[1] } } };
            gltfModel.images = modelContext.images;
            gltfModel.textures = modelContext.textures;
            gltfModel.materials = modelContext.materials;



            if (vmd != null)
            {
                Dictionary<string, List<BoneKeyFrame>> boneKeyFrames = new Dictionary<string, List<BoneKeyFrame>>();
                var character = MMDCharacter.Load(pmx);
                MMDMotion motion = VMDFormatExtension.Load(vmd);
                PhysicsScene scene = new PhysicsScene();
                if (options.usePhysics)
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
                    if (options.usePhysics)
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
                        boneKeyFrames[bone.Name].Add(new BoneKeyFrame { Frame = t, Translation = bone.translation, Rotation = bone.finalRotation });
                    }
                }

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
                        glTFWriterContext.CreateAccessor(keyFrames.Key + "/_keyFrameTime", keyFrames.Value.Select(u => u.Frame / 30.0f / c_sampleCount).ToArray(), null);
                    }
                    if (hasRots)
                    {
                        glTFWriterContext.CreateAccessor(keyFrames.Key + "/_keyFrameRotation", keyFrames.Value.Select(u => new Vector4(u.rotation.X, u.rotation.Y, u.rotation.Z, u.rotation.W)).ToArray(), null);
                    }
                    if (hasTrans)
                    {
                        glTFWriterContext.CreateAccessor(keyFrames.Key + "/_keyFrameTranslation", keyFrames.Value.Select(u => Vector3.Transform(u.Translation + relatePosition, exportTransform)).ToArray(), null);
                    }
                }


                List<GLTFAnimation> animations = new List<GLTFAnimation>();
                GLTFAnimation animation = new GLTFAnimation();
                animation.name = vmd.Name;
                List<GLTFAnimationSampler> samplers = new List<GLTFAnimationSampler>();
                List<GLTFAnimationChannel> channels = new List<GLTFAnimationChannel>();
                foreach (var keyFrames in boneKeyFrames)
                {
                    if (!name2Bone.TryGetValue(keyFrames.Key, out int index))
                    {
                        continue;
                    }
                    if (glTFWriterContext.accessorStart.TryGetValue(keyFrames.Key + "/_keyFrameRotation", out var rotate))
                    {
                        GLTFAnimationSampler sampler = new GLTFAnimationSampler();
                        sampler.input = glTFWriterContext.accessorStart[keyFrames.Key + "/_keyFrameTime"];
                        sampler.output = rotate;
                        GLTFAnimationChannel channel = new GLTFAnimationChannel();
                        channel.sampler = sampler;
                        channel.target = new GLTFAnimationChannelTarget
                        {
                            node = bones[index],
                            path = "rotation",
                        };
                        samplers.Add(sampler);
                        channels.Add(channel);
                    }
                    if (glTFWriterContext.accessorStart.TryGetValue(keyFrames.Key + "/_keyFrameTranslation", out var translate))
                    {
                        GLTFAnimationSampler sampler = new GLTFAnimationSampler();
                        sampler.input = glTFWriterContext.accessorStart[keyFrames.Key + "/_keyFrameTime"];
                        sampler.output = translate;
                        GLTFAnimationChannel channel = new GLTFAnimationChannel();
                        channel.sampler = sampler;
                        channel.target = new GLTFAnimationChannelTarget
                        {
                            node = bones[index],
                            path = "translation",
                        };
                        samplers.Add(sampler);
                        channels.Add(channel);
                    }
                }

                {

                    List<float[]> keyFrames1 = VMDSampler1.ProcessVertMorph(vmd, vertexMorphs, out float[] morphFrameTime);

                    for (int i = 0; i < keyFrames1.Count; i++)
                    {
                        for (int j = 0; j < keyFrames1[i].Length; j++)
                            writer.Write(keyFrames1[i][j]);
                    }
                    glTFWriterContext.MarkBuf("_keyFrameMorphWeight", new GLTFAccessor { componentType = 5126, count = keyFrames1.Count * vertexMorphs.Length, type = "SCALAR" });
                    glTFWriterContext.CreateAccessor("_keyFrameMorphTime", morphFrameTime);
                    GLTFAnimationSampler samplerMorph = new GLTFAnimationSampler();
                    samplerMorph.input = glTFWriterContext.accessorStart["_keyFrameMorphTime"];
                    samplerMorph.output = glTFWriterContext.accessorStart["_keyFrameMorphWeight"];
                    GLTFAnimationChannel channel = new GLTFAnimationChannel();
                    channel.sampler = samplerMorph;
                    channel.target = new GLTFAnimationChannelTarget
                    {
                        node = nodes[0],
                        path = "weights",
                    };
                    samplers.Add(samplerMorph);
                    channels.Add(channel);
                }

                animation.channels = channels.ToArray();
                animation.samplers = samplers.ToArray();
                animations.Add(animation);
                gltfModel.animations = animations.ToArray();
            }


            //List<GLTFPrimitive> morphPrimitives = new List<GLTFPrimitive>();

            //for (int i = 0; i < pmx.Materials.Count; i++)
            //{
            //    var primitive = modelContext.CreatePrimitive(exportTransform, pmx.Materials[i], true);
            //    if (primitive != null)
            //        morphPrimitives.Add(primitive);
            //}
            ////for (int i = 0; i < pmx.Materials.Count; i++)
            ////{
            ////    var primitive = modelContext.CreatePrimitive(exportTransform, pmx.Materials[i], false);
            ////    if (primitive != null)
            ////        morphPrimitives.Add(primitive);
            ////}
            //var morphMesh = new GLTFMesh
            //{
            //    name = pmx.Name,
            //    primitives = morphPrimitives.ToArray()
            //};
            //nodes[0].mesh = morphMesh;
            var mesh = CreateMesh(pmx, modelContext, exportTransform, true);
            var mesh1 = CreateMesh(pmx, modelContext, exportTransform, false);
            nodes[0].mesh = mesh;
            nodes[1].mesh = mesh1;
            gltfModel.meshes = new GLTFMesh[] { mesh, mesh1 };

            int totalSize = (int)bufferStream.Position;
            bufferStream.Flush();
            bufferStream.Dispose();
            glTFWriterContext.defaultBuffer.uri = Path.GetFileName(bufferFileName);
            glTFWriterContext.defaultBuffer.byteLength = totalSize;
            gltfModel.buffers = new[] { glTFWriterContext.defaultBuffer };
            gltfModel.bufferViews = glTFWriterContext.bufferViews.ToArray();
            gltfModel.accessors = glTFWriterContext.accessors.ToArray();
            gltfModel.asset.generator = "MMDMotionCompute";

            File.WriteAllBytes(path, gltfModel.ToBytes());

        }

        static GLTFMesh CreateMesh(PMXFormat pmx, ModelContext modelContext, Matrix4x4 exportTransform, bool morph)
        {
            List<GLTFPrimitive> morphPrimitives = new List<GLTFPrimitive>();

            for (int i = 0; i < pmx.Materials.Count; i++)
            {
                var primitive = modelContext.CreatePrimitive(exportTransform, pmx.Materials[i], morph);
                if (primitive != null)
                    morphPrimitives.Add(primitive);
            }
            return new GLTFMesh
            {
                name = pmx.Name,
                primitives = morphPrimitives.ToArray()
            };
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
    }
}

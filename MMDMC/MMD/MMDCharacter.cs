﻿using MMDMC.Physics;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MMDMC.MMD
{
    public class MMDCharacter
    {

        public List<Bone> bones = new List<Bone>();

        public Dictionary<int, List<List<int>>> IKNeedUpdateIndexs;
        public List<int> AppendNeedUpdateMatIndexs = new List<int>();
        public List<int> PhysicsNeedUpdateMatIndexs = new List<int>();

        public List<PhysicsJoint> joints;
        public List<PhysicsRigidBody> rigidBodys;

        public Matrix4x4 LocalToWorld = Matrix4x4.Identity;
        public Matrix4x4 WorldToLocal = Matrix4x4.Identity;

        public List<PMX_RigidBody> rigidBodyDescs;
        public List<PMX_Joint> jointDescs;

        MorphStateComponent morphStateComponent;

        public void PrePhysicsSync(PhysicsScene physics3DScene)
        {
            for (int i = 0; i < rigidBodyDescs.Count; i++)
            {
                var desc = rigidBodyDescs[i];
                if (desc.Type != 0) continue;
                int index = desc.AssociatedBoneIndex;

                Matrix4x4 mat2 = Matrix4x4.CreateFromQuaternion(ToQuaternion(desc.Rotation)) * Matrix4x4.CreateTranslation(desc.Position) * bones[index].GeneratedTransform * LocalToWorld;
                physics3DScene.MoveRigidBody(rigidBodys[i], mat2);

            }
        }

        public void PhysicsSync(PhysicsScene physics3DScene)
        {
            Matrix4x4.Decompose(WorldToLocal, out _, out var q1, out var t1);
            for (int i = 0; i < rigidBodyDescs.Count; i++)
            {
                var desc = rigidBodyDescs[i];
                if (desc.Type == 0) continue;
                int index = desc.AssociatedBoneIndex;
                if (index == -1) continue;
                bones[index].GetPosRot2(out Vector3 pos, out Quaternion rot);
                bones[index]._generatedTransform = Matrix4x4.CreateTranslation(-desc.Position) * Matrix4x4.CreateFromQuaternion(rigidBodys[i].GetRotation() / ToQuaternion(desc.Rotation) * q1)
                    * Matrix4x4.CreateTranslation(Vector3.Transform(rigidBodys[i].GetPosition(), WorldToLocal));
            }
            UpdateMatrices(PhysicsNeedUpdateMatIndexs);

            UpdateAppendBones();
            for (int i = 0; i < rigidBodyDescs.Count; i++)
            {
                var desc = rigidBodyDescs[i];
                if (desc.Type == 0)
                    continue;
                int index = desc.AssociatedBoneIndex;
                if (index == -1)
                    continue;
                bones[index].GetPosRot2(out Vector3 pos, out Quaternion rot);
                Vector3 pos1 = new Vector3();
                Quaternion rot1 = Quaternion.Identity;

                Vector3 parentStaticPosition = new Vector3();
                int parent = bones[index].ParentIndex;
                if (parent != -1)
                {
                    bones[parent].GetPosRot2(out pos1, out rot1);
                    parentStaticPosition = bones[parent].staticPosition;
                }

                bones[index].translation = Vector3.Transform(pos - pos1, Quaternion.Identity / rot1) + parentStaticPosition - bones[index].staticPosition;
                bones[index].rotation = Quaternion.Identity / rot1 * rot;
            }
        }

        public void ResetPhysics(PhysicsScene physics3DScene)
        {
            UpdateAllMatrix();
            for (int i = 0; i < rigidBodyDescs.Count; i++)
            {
                var desc = rigidBodyDescs[i];
                if (desc.Type == 0) continue;
                int index = desc.AssociatedBoneIndex;
                if (index == -1) continue;
                var mat1 = bones[index].GeneratedTransform * LocalToWorld;
                Matrix4x4.Decompose(mat1, out _, out var rot, out _);
                physics3DScene.ResetRigidBody(rigidBodys[i], Vector3.Transform(desc.Position, mat1), rot * ToQuaternion(desc.Rotation));
            }
        }

        public void BakeSequenceProcessMatrixsIndex()
        {
            IKNeedUpdateIndexs = new Dictionary<int, List<List<int>>>();
            bool[] bonesTest = new bool[bones.Count];

            for (int i = 0; i < bones.Count; i++)
            {
                int ikTargetIndex = bones[i].IKTargetIndex;
                if (ikTargetIndex != -1)
                {
                    List<List<int>> ax = new List<List<int>>();
                    var entity = bones[i];
                    var entitySource = bones[ikTargetIndex];
                    for (int j = 0; j < entity.boneIKLinks.Length; j++)
                    {
                        List<int> bx = new List<int>();

                        Array.Clear(bonesTest, 0, bones.Count);
                        bonesTest[entity.boneIKLinks[j].LinkedIndex] = true;
                        for (int k = 0; k < bones.Count; k++)
                        {
                            if (bones[k].ParentIndex != -1)
                            {
                                bonesTest[k] |= bonesTest[bones[k].ParentIndex];
                                if (bonesTest[k])
                                    bx.Add(k);
                            }
                        }
                        ax.Add(bx);
                    }
                    IKNeedUpdateIndexs[i] = ax;
                }
            }
            Array.Clear(bonesTest, 0, bones.Count);
            AppendNeedUpdateMatIndexs.Clear();
            for (int i = 0; i < bones.Count; i++)
            {
                var bone = bones[i];
                if (bones[i].ParentIndex != -1)
                    bonesTest[i] |= bonesTest[bones[i].ParentIndex];
                bonesTest[i] |= bone.IsAppendTranslation || bone.IsAppendRotation;
                if (bonesTest[i])
                {
                    AppendNeedUpdateMatIndexs.Add(i);
                }
            }
            Array.Clear(bonesTest, 0, bones.Count);
            PhysicsNeedUpdateMatIndexs.Clear();
            for (int i = 0; i < bones.Count; i++)
            {
                var bone = bones[i];
                if (bones[i].ParentIndex == -1)
                    continue;
                var parent = bones[bones[i].ParentIndex];
                bonesTest[i] |= bonesTest[bones[i].ParentIndex];
                if (bonesTest[i])
                {
                    PhysicsNeedUpdateMatIndexs.Add(i);
                }
            }
        }

        void IK(int boneIndex, List<Bone> bones)
        {

            int ikTargetIndex = bones[boneIndex].IKTargetIndex;
            if (ikTargetIndex == -1) return;
            var entity = bones[boneIndex];
            var entitySource = bones[ikTargetIndex];

            entity.GetPosRot2(out var posTarget, out var rot0);


            int h1 = entity.CCDIterateLimit / 2;
            Vector3 posSource = entitySource.GetPos2();
            if ((posTarget - posSource).LengthSquared() < 1e-6f)
                return;
            for (int i = 0; i < entity.CCDIterateLimit; i++)
            {
                bool axis_lim = i < h1;
                for (int j = 0; j < entity.boneIKLinks.Length; j++)
                {
                    posSource = entitySource.GetPos2();
                    ref var IKLINK = ref entity.boneIKLinks[j];
                    Bone itEntity = bones[IKLINK.LinkedIndex];

                    itEntity.GetPosRot2(out var itPosition, out var itRot);

                    Vector3 targetDirection = Vector3.Normalize(itPosition - posTarget);
                    Vector3 ikDirection = Vector3.Normalize(itPosition - posSource);
                    float dotV = Math.Clamp(Vector3.Dot(targetDirection, ikDirection), -1, 1);
                    float angle1 = (float)Math.Acos(dotV);
                    if (Math.Abs(angle1) < 1e-3f)
                    {
                        continue;
                    }

                    Matrix4x4 matXi = Matrix4x4.Transpose(itEntity.GeneratedTransform);
                    Vector3 ikRotateAxis = Vector3.Normalize(Vector3.TransformNormal(Vector3.Cross(targetDirection, ikDirection), matXi));

                    //if (axis_lim)
                    //    switch (IKLINK.FixTypes)
                    //    {
                    //        case AxisFixType.FixX:
                    //            ikRotateAxis.X = ikRotateAxis.X >= 0 ? 1 : -1;
                    //            ikRotateAxis.Y = 0;
                    //            ikRotateAxis.Z = 0;
                    //            break;
                    //        case AxisFixType.FixY:
                    //            ikRotateAxis.X = 0;
                    //            ikRotateAxis.Y = ikRotateAxis.Y >= 0 ? 1 : -1;
                    //            ikRotateAxis.Z = 0;
                    //            break;
                    //        case AxisFixType.FixZ:
                    //            ikRotateAxis.X = 0;
                    //            ikRotateAxis.Y = 0;
                    //            ikRotateAxis.Z = ikRotateAxis.Z >= 0 ? 1 : -1;
                    //            break;
                    //    }
                    //重命名函数以缩短函数名
                    Quaternion QAxisAngle(Vector3 axis, float angle) => Quaternion.CreateFromAxisAngle(axis, angle);

                    itEntity.rotation = Quaternion.Normalize(itEntity.rotation * QAxisAngle(ikRotateAxis, -Math.Min((float)Math.Acos(dotV), entity.CCDAngleLimit * (i + 1))));

                    if (IKLINK.HasLimit)
                    {
                        Vector3 angle = Vector3.Zero;
                        switch (IKLINK.TransformOrder)
                        {
                            case IKTransformOrder.Zxy:
                                {
                                    angle = MathHelper.QuaternionToZxy(itEntity.rotation);
                                    Vector3 cachedE = angle;
                                    angle = LimitAngle(angle, axis_lim, IKLINK.LimitMin, IKLINK.LimitMax);
                                    if (cachedE != angle)
                                        itEntity.rotation = Quaternion.Normalize(QAxisAngle(Vector3.UnitZ, angle.Z) * QAxisAngle(Vector3.UnitX, angle.X) * QAxisAngle(Vector3.UnitY, angle.Y));
                                    break;
                                }
                            case IKTransformOrder.Xyz:
                                {
                                    angle = MathHelper.QuaternionToXyz(itEntity.rotation);
                                    Vector3 cachedE = angle;
                                    angle = LimitAngle(angle, axis_lim, IKLINK.LimitMin, IKLINK.LimitMax);
                                    if (cachedE != angle)
                                        itEntity.rotation = Quaternion.Normalize(QAxisAngle(Vector3.UnitX, angle.X) * QAxisAngle(Vector3.UnitY, angle.Y) * QAxisAngle(Vector3.UnitZ, angle.Z));
                                    break;
                                }
                            case IKTransformOrder.Yzx:
                                {
                                    angle = MathHelper.QuaternionToYzx(itEntity.rotation);
                                    Vector3 cachedE = angle;
                                    angle = LimitAngle(angle, axis_lim, IKLINK.LimitMin, IKLINK.LimitMax);
                                    if (cachedE != angle)
                                        itEntity.rotation = Quaternion.Normalize(QAxisAngle(Vector3.UnitY, angle.Y) * QAxisAngle(Vector3.UnitZ, angle.Z) * QAxisAngle(Vector3.UnitX, angle.X));
                                    break;
                                }
                            default:
                                throw new NotImplementedException();
                        }
                    }
                    UpdateMatrices(IKNeedUpdateIndexs[boneIndex][j]);
                }
                posSource = entitySource.GetPos2();
                if ((posTarget - posSource).LengthSquared() < 1e-6f)
                    return;
            }
        }

        void UpdateAllMatrix()
        {
            for (int i = 0; i < bones.Count; i++)
                bones[i].GetTransformMatrixG(bones);
        }
        void UpdateMatrices(List<int> indexs)
        {
            for (int i = 0; i < indexs.Count; i++)
                bones[indexs[i]].GetTransformMatrixG(bones);
        }

        void UpdateAppendBones()
        {
            for (int i = 0; i < bones.Count; i++)
            {
                var bone = bones[i];
                if (bone.IsAppendTranslation || bone.IsAppendRotation)
                {
                    var mat1 = bones[bone.AppendParentIndex].GeneratedTransform;
                    if (bone.IsAppendTranslation)
                    {
                        bone.appendTranslation = bones[bone.AppendParentIndex].translation * bone.AppendRatio;
                    }
                    if (bone.IsAppendRotation)
                    {
                        bone.appendRotation = Quaternion.Slerp(Quaternion.Identity, bones[bone.AppendParentIndex].rotation, bone.AppendRatio);
                    }
                }
            }
            UpdateMatrices(AppendNeedUpdateMatIndexs);
        }

        void BoneMorphIKAppend()
        {
            for (int i = 0; i < morphStateComponent.morphs.Count; i++)
            {
                if (morphStateComponent.morphs[i].Type == PMX_MorphType.Bone)
                {
                    PMX_MorphBoneDesc[] morphBoneStructs = morphStateComponent.morphs[i].MorphBones;
                    float computedWeight = morphStateComponent.Weights.Computed[i];
                    for (int j = 0; j < morphBoneStructs.Length; j++)
                    {
                        var morphBoneStruct = morphBoneStructs[j];
                        bones[morphBoneStruct.BoneIndex].rotation *= Quaternion.Slerp(Quaternion.Identity, morphBoneStruct.Rotation, computedWeight);
                        bones[morphBoneStruct.BoneIndex].translation += morphBoneStruct.Translation * computedWeight;
                    }
                }
            }

            for (int i = 0; i < bones.Count; i++)
            {
                IK(i, bones);
            }
            UpdateAppendBones();
        }

        public void SetMotionTime(float time, MMDMotion motionComponent)
        {
            morphStateComponent.SetPose(motionComponent, time);
            morphStateComponent.ComputeWeight();
            foreach (var bone in bones)
            {
                bone.appendTranslation = Vector3.Zero;
                bone.appendRotation = Quaternion.Identity;
                var keyframe = motionComponent.GetBoneMotion(bone.Name, time);
                bone.rotation = keyframe.rotation;
                bone.translation = keyframe.translation;
                //cachedBoneKeyFrames[bone.index] = keyframe;
            }
            UpdateAllMatrix();
            BoneMorphIKAppend();
            //VertexMaterialMorph();
        }


        public void AddPhysics(PhysicsScene physics3DScene)
        {
            rigidBodys = new List<PhysicsRigidBody>();
            joints = new List<PhysicsJoint>();
            for (int j = 0; j < rigidBodyDescs.Count; j++)
            {
                rigidBodys.Add(new PhysicsRigidBody());
                var desc = rigidBodyDescs[j];
                physics3DScene.AddRigidBody(rigidBodys[j], desc);
            }
            for (int j = 0; j < jointDescs.Count; j++)
            {
                joints.Add(new PhysicsJoint());
                var desc = jointDescs[j];
                physics3DScene.AddJoint(joints[j], rigidBodys[desc.AssociatedRigidBodyIndex1], rigidBodys[desc.AssociatedRigidBodyIndex2], desc);
            }
        }

        private Vector3 LimitAngle(Vector3 angle, bool axis_lim, Vector3 low, Vector3 high)
        {
            if (!axis_lim)
            {
                return Vector3.Clamp(angle, low, high);
            }
            Vector3 vecL1 = 2.0f * low - angle;
            Vector3 vecH1 = 2.0f * high - angle;
            if (angle.X < low.X)
            {
                angle.X = (vecL1.X <= high.X) ? vecL1.X : low.X;
            }
            else if (angle.X > high.X)
            {
                angle.X = (vecH1.X >= low.X) ? vecH1.X : high.X;
            }
            if (angle.Y < low.Y)
            {
                angle.Y = (vecL1.Y <= high.Y) ? vecL1.Y : low.Y;
            }
            else if (angle.Y > high.Y)
            {
                angle.Y = (vecH1.Y >= low.Y) ? vecH1.Y : high.Y;
            }
            if (angle.Z < low.Z)
            {
                angle.Z = (vecL1.Z <= high.Z) ? vecL1.Z : low.Z;
            }
            else if (angle.Z > high.Z)
            {
                angle.Z = (vecH1.Z >= low.Z) ? vecH1.Z : high.Z;
            }
            return angle;
        }

        public static MMDCharacter Load(PMXFormat pmx)
        {
            var charater = new MMDCharacter();
            for (int i = 0; i < pmx.Bones.Count; i++)
            {
                var _bone = pmx.Bones[i];
                var bone = new Bone()
                {
                    index = i,
                    ParentIndex = (_bone.ParentIndex >= 0 && _bone.ParentIndex < pmx.Bones.Count) ? _bone.ParentIndex : -1,
                    staticPosition = _bone.Position,
                    rotation = Quaternion.Identity,
                    Name = _bone.Name,
                    Flags = _bone.Flags,
                    NameEN = _bone.NameEN,
                };

                if (bone.Flags.HasFlag(PMX_BoneFlag.HasIK))
                {
                    bone.IKTargetIndex = _bone.boneIK.IKTargetIndex;
                    bone.CCDIterateLimit = _bone.boneIK.CCDIterateLimit;
                    bone.CCDAngleLimit = _bone.boneIK.CCDAngleLimit;
                    bone.boneIKLinks = new IKLink[_bone.boneIK.IKLinks.Length];

                    for (int j = 0; j < bone.boneIKLinks.Length; j++)
                    {
                        var ikLink = new IKLink();
                        ikLink.HasLimit = _bone.boneIK.IKLinks[j].HasLimit;
                        ikLink.LimitMax = _bone.boneIK.IKLinks[j].LimitMax;
                        ikLink.LimitMin = _bone.boneIK.IKLinks[j].LimitMin;
                        ikLink.LinkedIndex = _bone.boneIK.IKLinks[j].LinkedIndex;

                        Vector3 tempMin = ikLink.LimitMin;
                        Vector3 tempMax = ikLink.LimitMax;
                        ikLink.LimitMin = Vector3.Min(tempMin, tempMax);
                        ikLink.LimitMax = Vector3.Max(tempMin, tempMax);

                        if (ikLink.LimitMin.X > -Math.PI * 0.5 && ikLink.LimitMax.X < Math.PI * 0.5)
                            ikLink.TransformOrder = IKTransformOrder.Zxy;
                        else if (ikLink.LimitMin.Y > -Math.PI * 0.5 && ikLink.LimitMax.Y < Math.PI * 0.5)
                            ikLink.TransformOrder = IKTransformOrder.Xyz;
                        else
                            ikLink.TransformOrder = IKTransformOrder.Yzx;

                        bone.boneIKLinks[j] = ikLink;
                    }
                }
                if (_bone.AppendBoneIndex >= 0 && _bone.AppendBoneIndex < pmx.Bones.Count)
                {
                    bone.AppendParentIndex = _bone.AppendBoneIndex;
                    bone.AppendRatio = _bone.AppendBoneRatio;
                    bone.IsAppendRotation = bone.Flags.HasFlag(PMX_BoneFlag.AcquireRotate);
                    bone.IsAppendTranslation = bone.Flags.HasFlag(PMX_BoneFlag.AcquireTranslate);
                }
                else
                {
                    bone.AppendParentIndex = -1;
                    bone.AppendRatio = 0;
                    bone.IsAppendRotation = false;
                    bone.IsAppendTranslation = false;
                }
                charater.bones.Add(bone);
            }
            charater.rigidBodyDescs = pmx.RigidBodies;
            charater.jointDescs = pmx.Joints;
            charater.BakeSequenceProcessMatrixsIndex();
            charater.morphStateComponent = MorphStateComponent.LoadMorphStateComponent(pmx);
            return charater;
        }

        public class Bone
        {
            public Vector3 translation;
            public Quaternion rotation;
            public Vector3 staticPosition;
            public Vector3 appendTranslation;
            public Quaternion appendRotation = Quaternion.Identity;
            public int index;
            public Quaternion finalRotation { get => rotation * appendRotation; }

            public int CCDIterateLimit = 0;
            public float CCDAngleLimit = 0;

            public int ParentIndex = -1;
            public int IKTargetIndex = -1;
            public int AppendParentIndex = -1;
            public string Name;
            public string NameEN;
            public bool IsAppendRotation;
            public bool IsAppendTranslation;
            public PMX_BoneFlag Flags;
            public IKLink[] boneIKLinks;
            public float AppendRatio;

            public Matrix4x4 _generatedTransform = Matrix4x4.Identity;
            public Matrix4x4 GeneratedTransform { get => _generatedTransform; }

            public void GetTransformMatrixG(List<Bone> list)
            {
                if (ParentIndex != -1)
                {
                    _generatedTransform = Matrix4x4.CreateTranslation(-staticPosition) *
                       Matrix4x4.CreateFromQuaternion(rotation * appendRotation) *
                       Matrix4x4.CreateTranslation(staticPosition + appendTranslation + translation) * list[ParentIndex]._generatedTransform;
                }
                else
                {
                    _generatedTransform = Matrix4x4.CreateTranslation(-staticPosition) *
                       Matrix4x4.CreateFromQuaternion(rotation * appendRotation) *
                       Matrix4x4.CreateTranslation(staticPosition + appendTranslation + translation);
                }
            }
            public Vector3 GetPos2()
            {
                return Vector3.Transform(staticPosition, _generatedTransform);
            }

            public void GetPosRot2(out Vector3 pos, out Quaternion rot)
            {
                pos = Vector3.Transform(staticPosition, _generatedTransform);
                Matrix4x4.Decompose(_generatedTransform, out _, out rot, out _);
            }
        }

        public enum IKTransformOrder
        {
            Yzx = 0,
            Zxy = 1,
            Xyz = 2,
        }
        public struct IKLink
        {
            public int LinkedIndex;
            public bool HasLimit;
            public Vector3 LimitMin;
            public Vector3 LimitMax;
            public IKTransformOrder TransformOrder;
            //public AxisFixType FixTypes;
        }

        public static Quaternion ToQuaternion(Vector3 angle)
        {
            return Quaternion.CreateFromYawPitchRoll(angle.Y, angle.X, angle.Z);
        }
    }

    public static class MathHelper
    {
        public static Vector3 QuaternionToXyz(Quaternion quaternion)
        {
            double ii = quaternion.X * quaternion.X;
            double jj = quaternion.Y * quaternion.Y;
            double kk = quaternion.Z * quaternion.Z;
            double ei = quaternion.W * quaternion.X;
            double ej = quaternion.W * quaternion.Y;
            double ek = quaternion.W * quaternion.Z;
            double ij = quaternion.X * quaternion.Y;
            double ik = quaternion.X * quaternion.Z;
            double jk = quaternion.Y * quaternion.Z;
            Vector3 result = new Vector3();
            result.X = (float)Math.Atan2(2.0f * (ei - jk), 1 - 2.0f * (ii + jj));
            result.Y = (float)Math.Asin(2.0f * (ej + ik));
            result.Z = (float)Math.Atan2(2.0f * (ek - ij), 1 - 2.0f * (jj + kk));
            return result;
        }
        public static Vector3 QuaternionToXzy(Quaternion quaternion)
        {
            double ii = quaternion.X * quaternion.X;
            double jj = quaternion.Y * quaternion.Y;
            double kk = quaternion.Z * quaternion.Z;
            double ei = quaternion.W * quaternion.X;
            double ej = quaternion.W * quaternion.Y;
            double ek = quaternion.W * quaternion.Z;
            double ij = quaternion.X * quaternion.Y;
            double ik = quaternion.X * quaternion.Z;
            double jk = quaternion.Y * quaternion.Z;
            Vector3 result = new Vector3();
            result.X = (float)Math.Atan2(2.0f * (ei + jk), 1 - 2.0f * (ii + kk));
            result.Y = (float)Math.Atan2(2.0f * (ej + ik), 1 - 2.0f * (jj + kk));
            result.Z = (float)Math.Asin(2.0f * (ek - ij));
            return result;
        }
        public static Vector3 QuaternionToYxz(Quaternion quaternion)
        {
            double ii = quaternion.X * quaternion.X;
            double jj = quaternion.Y * quaternion.Y;
            double kk = quaternion.Z * quaternion.Z;
            double ei = quaternion.W * quaternion.X;
            double ej = quaternion.W * quaternion.Y;
            double ek = quaternion.W * quaternion.Z;
            double ij = quaternion.X * quaternion.Y;
            double ik = quaternion.X * quaternion.Z;
            double jk = quaternion.Y * quaternion.Z;
            Vector3 result = new Vector3();
            result.X = (float)Math.Asin(2.0f * (ei - jk));
            result.Y = (float)Math.Atan2(2.0f * (ej + ik), 1 - 2.0f * (ii + jj));
            result.Z = (float)Math.Atan2(2.0f * (ek + ij), 1 - 2.0f * (ii + kk));
            return result;
        }
        public static Vector3 QuaternionToYzx(Quaternion quaternion)
        {
            double ii = quaternion.X * quaternion.X;
            double jj = quaternion.Y * quaternion.Y;
            double kk = quaternion.Z * quaternion.Z;
            double ei = quaternion.W * quaternion.X;
            double ej = quaternion.W * quaternion.Y;
            double ek = quaternion.W * quaternion.Z;
            double ij = quaternion.X * quaternion.Y;
            double ik = quaternion.X * quaternion.Z;
            double jk = quaternion.Y * quaternion.Z;
            Vector3 result = new Vector3();
            result.X = (float)Math.Atan2(2.0f * (ei - jk), 1 - 2.0f * (ii + kk));
            result.Y = (float)Math.Atan2(2.0f * (ej - ik), 1 - 2.0f * (jj + kk));
            result.Z = (float)Math.Asin(2.0f * (ek + ij));
            return result;
        }
        public static Vector3 QuaternionToZxy(Quaternion quaternion)
        {
            double ii = quaternion.X * quaternion.X;
            double jj = quaternion.Y * quaternion.Y;
            double kk = quaternion.Z * quaternion.Z;
            double ei = quaternion.W * quaternion.X;
            double ej = quaternion.W * quaternion.Y;
            double ek = quaternion.W * quaternion.Z;
            double ij = quaternion.X * quaternion.Y;
            double ik = quaternion.X * quaternion.Z;
            double jk = quaternion.Y * quaternion.Z;
            Vector3 result = new Vector3();
            result.X = (float)Math.Asin(2.0f * (ei + jk));
            result.Y = (float)Math.Atan2(2.0f * (ej - ik), 1 - 2.0f * (ii + jj));
            result.Z = (float)Math.Atan2(2.0f * (ek - ij), 1 - 2.0f * (ii + kk));
            return result;
        }
        public static Vector3 QuaternionToZyx(Quaternion quaternion)
        {
            double ii = quaternion.X * quaternion.X;
            double jj = quaternion.Y * quaternion.Y;
            double kk = quaternion.Z * quaternion.Z;
            double ei = quaternion.W * quaternion.X;
            double ej = quaternion.W * quaternion.Y;
            double ek = quaternion.W * quaternion.Z;
            double ij = quaternion.X * quaternion.Y;
            double ik = quaternion.X * quaternion.Z;
            double jk = quaternion.Y * quaternion.Z;
            Vector3 result = new Vector3();
            result.X = (float)Math.Atan2(2.0f * (ei + jk), 1 - 2.0f * (ii + jj));
            result.Y = (float)Math.Asin(2.0f * (ej - ik));
            result.Z = (float)Math.Atan2(2.0f * (ek + ij), 1 - 2.0f * (jj + kk));
            return result;
        }

        public static Quaternion XyzToQuaternion(Vector3 euler)
        {
            double cx = Math.Cos(euler.X * 0.5f);
            double sx = Math.Sin(euler.X * 0.5f);
            double cy = Math.Cos(euler.Y * 0.5f);
            double sy = Math.Sin(euler.Y * 0.5f);
            double cz = Math.Cos(euler.Z * 0.5f);
            double sz = Math.Sin(euler.Z * 0.5f);
            Quaternion result;
            result.W = (float)(cx * cy * cz - sx * sy * sz);
            result.X = (float)(sx * cy * cz + cx * sy * sz);
            result.Y = (float)(cx * sy * cz - sx * cy * sz);
            result.Z = (float)(sx * sy * cz + cx * cy * sz);
            return result;
        }
        public static Quaternion XzyToQuaternion(Vector3 euler)
        {
            double cx = Math.Cos(euler.X * 0.5f);
            double sx = Math.Sin(euler.X * 0.5f);
            double cy = Math.Cos(euler.Y * 0.5f);
            double sy = Math.Sin(euler.Y * 0.5f);
            double cz = Math.Cos(euler.Z * 0.5f);
            double sz = Math.Sin(euler.Z * 0.5f);
            Quaternion result;
            result.W = (float)(cx * cy * cz + sx * sy * sz);
            result.X = (float)(sx * cy * cz - cx * sy * sz);
            result.Y = (float)(cx * sy * cz - sx * cy * sz);
            result.Z = (float)(cx * cy * sz + sx * sy * cz);
            return result;
        }
        public static Quaternion YxzToQuaternion(Vector3 euler)
        {
            double cx = Math.Cos(euler.X * 0.5f);
            double sx = Math.Sin(euler.X * 0.5f);
            double cy = Math.Cos(euler.Y * 0.5f);
            double sy = Math.Sin(euler.Y * 0.5f);
            double cz = Math.Cos(euler.Z * 0.5f);
            double sz = Math.Sin(euler.Z * 0.5f);
            Quaternion result;
            result.W = (float)(cx * cy * cz + sx * sy * sz);
            result.X = (float)(sx * cy * cz + cx * sy * sz);
            result.Y = (float)(cx * sy * cz - sx * cy * sz);
            result.Z = (float)(cx * cy * sz - sx * sy * cz);
            return result;
        }
        public static Quaternion YzxToQuaternion(Vector3 euler)
        {
            double cx = Math.Cos(euler.X * 0.5f);
            double sx = Math.Sin(euler.X * 0.5f);
            double cy = Math.Cos(euler.Y * 0.5f);
            double sy = Math.Sin(euler.Y * 0.5f);
            double cz = Math.Cos(euler.Z * 0.5f);
            double sz = Math.Sin(euler.Z * 0.5f);
            Quaternion result;
            result.W = (float)(cx * cy * cz - sx * sy * sz);
            result.X = (float)(sx * cy * cz + cx * sy * sz);
            result.Y = (float)(cx * sy * cz + sx * cy * sz);
            result.Z = (float)(cx * cy * sz - sx * sy * cz);
            return result;
        }
        public static Quaternion ZxyToQuaternion(Vector3 euler)
        {
            double cx = Math.Cos(euler.X * 0.5f);
            double sx = Math.Sin(euler.X * 0.5f);
            double cy = Math.Cos(euler.Y * 0.5f);
            double sy = Math.Sin(euler.Y * 0.5f);
            double cz = Math.Cos(euler.Z * 0.5f);
            double sz = Math.Sin(euler.Z * 0.5f);
            Quaternion result;
            result.W = (float)(cx * cy * cz - sx * sy * sz);
            result.X = (float)(sx * cy * cz - cx * sy * sz);
            result.Y = (float)(cx * sy * cz + sx * cy * sz);
            result.Z = (float)(cx * cy * sz + sx * sy * cz);
            return result;
        }
        public static Quaternion ZYXToQuaternion(Vector3 euler)
        {
            double cx = Math.Cos(euler.X * 0.5f);
            double sx = Math.Sin(euler.X * 0.5f);
            double cy = Math.Cos(euler.Y * 0.5f);
            double sy = Math.Sin(euler.Y * 0.5f);
            double cz = Math.Cos(euler.Z * 0.5f);
            double sz = Math.Sin(euler.Z * 0.5f);
            Quaternion result;
            result.W = (float)(cx * cy * cz + sx * sy * sz);
            result.X = (float)(sx * cy * cz - cx * sy * sz);
            result.Y = (float)(cx * sy * cz + sx * cy * sz);
            result.Z = (float)(cx * cy * sz - sx * sy * cz);
            return result;
        }
    }

    public class MorphStateComponent
    {
        public List<MorphDesc> morphs = new List<MorphDesc>();
        public WeightGroup Weights = new WeightGroup();

        public const float c_frameInterval = 1 / 30.0f;
        public Dictionary<string, int> stringMorphIndexMap = new Dictionary<string, int>();
        public void SetPose(MMDMotion motionComponent, float time)
        {
            float currentTimeA = MathF.Floor(time / c_frameInterval) * c_frameInterval;
            foreach (var pair in stringMorphIndexMap)
            {
                Weights.Origin[pair.Value] = motionComponent.GetMorphWeight(pair.Key, time);
            }
        }
        public void SetPoseDefault()
        {
            foreach (var pair in stringMorphIndexMap)
            {
                Weights.Origin[pair.Value] = 0;
            }
        }

        public void ComputeWeight()
        {
            ComputeWeight1(morphs, Weights);
        }

        private static void ComputeWeight1(IReadOnlyList<MorphDesc> morphs, WeightGroup weightGroup)
        {
            for (int i = 0; i < morphs.Count; i++)
            {
                weightGroup.ComputedPrev[i] = weightGroup.Computed[i];
                weightGroup.Computed[i] = 0;
            }
            for (int i = 0; i < morphs.Count; i++)
            {
                MorphDesc morph = morphs[i];
                if (morph.Type == PMX_MorphType.Group)
                    ComputeWeightGroup(morphs, morph, weightGroup.Origin[i], weightGroup.Computed);
                else
                    weightGroup.Computed[i] += weightGroup.Origin[i];
            }
        }
        private static void ComputeWeightGroup(IReadOnlyList<MorphDesc> morphs, MorphDesc morph, float rate, float[] computedWeights)
        {
            for (int i = 0; i < morph.SubMorphs.Length; i++)
            {
                PMX_MorphSubMorphDesc subMorphStruct = morph.SubMorphs[i];
                MorphDesc subMorph = morphs[subMorphStruct.GroupIndex];
                if (subMorph.Type == PMX_MorphType.Group)
                    ComputeWeightGroup(morphs, subMorph, rate * subMorphStruct.Rate, computedWeights);
                else
                    computedWeights[subMorphStruct.GroupIndex] += rate * subMorphStruct.Rate;
            }
        }

        public static MorphDesc GetMorphDesc(PMX_Morph desc)
        {

            return new MorphDesc()
            {
                Name = desc.Name,
                NameEN = desc.NameEN,
                Category = desc.Category,
                Type = desc.Type,
                MorphBones = desc.MorphBones,
                MorphMaterials = desc.MorphMaterials,
                MorphUVs = desc.MorphUVs,
                MorphVertexs = desc.MorphVertice,
                SubMorphs = desc.SubMorphs,
            };
        }
        public static MorphStateComponent LoadMorphStateComponent(PMXFormat pmx)
        {
            MorphStateComponent component = new MorphStateComponent();
            component.Reload(pmx);
            return component;
        }

        public void Reload(PMXFormat pmx)
        {
            MorphStateComponent component = this;
            component.stringMorphIndexMap.Clear();
            component.morphs.Clear();
            int morphCount = pmx.Morphs.Count;
            for (int i = 0; i < pmx.Morphs.Count; i++)
            {
                component.morphs.Add(GetMorphDesc(pmx.Morphs[i]));
            }

            void newWeightGroup(WeightGroup weightGroup)
            {
                weightGroup.Origin = new float[morphCount];
                weightGroup.Computed = new float[morphCount];
                weightGroup.ComputedPrev = new float[morphCount];
            }
            newWeightGroup(component.Weights);
            for (int i = 0; i < morphCount; i++)
            {
                component.stringMorphIndexMap.Add(pmx.Morphs[i].Name, i);
            }
        }
    }

    public class WeightGroup
    {
        public float[] Origin;
        public float[] Computed;
        public float[] ComputedPrev;

        public bool ComputedWeightNotEqualsPrev(int index)
        {
            return Computed[index] != ComputedPrev[index];
        }
    }

    public class MorphDesc
    {
        public string Name;
        public string NameEN;
        public PMX_MorphCategory Category;
        public PMX_MorphType Type;

        public PMX_MorphSubMorphDesc[] SubMorphs;
        public PMX_MorphVertexDesc[] MorphVertexs;
        public PMX_MorphBoneDesc[] MorphBones;
        public PMX_MorphUVDesc[] MorphUVs;
        public PMX_MorphMaterialDesc[] MorphMaterials;

        public override string ToString()
        {
            return string.Format("{0}", Name);
        }
    }
}

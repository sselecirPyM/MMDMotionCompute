using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MMDMotionCompute.MMD
{
    public class MMDCharacter
    {

        public List<Bone> bones = new List<Bone>();

        public Dictionary<int, List<List<int>>> IKNeedUpdateIndexs;
        public List<int> AppendNeedUpdateMatIndexs = new List<int>();
        public List<int> PhysicsNeedUpdateMatIndexs = new List<int>();

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
                bonesTest[i] |= parent.IsPhysicsFreeBone;
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
            if ((posTarget - posSource).LengthSquared() < 1e-8f) return;
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

                    Matrix4x4 matXi = Matrix4x4.Transpose(itEntity.GeneratedTransform);
                    Vector3 ikRotateAxis = SafeNormalize(Vector3.TransformNormal(Vector3.Cross(targetDirection, ikDirection), matXi));

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
                if ((posTarget - posSource).LengthSquared() < 1e-8f) return;
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
                    Matrix4x4.Decompose(mat1, out _, out var rotation, out var translation);
                    if (bone.IsAppendTranslation)
                    {
                        bone.appendTranslation = translation * bone.AppendRatio;
                    }
                    if (bone.IsAppendRotation)
                    {
                        bone.appendRotation = Quaternion.Slerp(Quaternion.Identity, bones[bone.AppendParentIndex].rotation, bone.AppendRatio);
                    }
                }
            }
            UpdateMatrices(AppendNeedUpdateMatIndexs);
        }

        public void SetPose3()
        {
            //for (int i = 0; i < morphStateComponent.morphs.Count; i++)
            //{
            //    if (morphStateComponent.morphs[i].Type == MorphType.Bone)
            //    {
            //        MorphBoneDesc[] morphBoneStructs = morphStateComponent.morphs[i].MorphBones;
            //        float computedWeight = morphStateComponent.Weights.Computed[i];
            //        for (int j = 0; j < morphBoneStructs.Length; j++)
            //        {
            //            var morphBoneStruct = morphBoneStructs[j];
            //            bones[morphBoneStruct.BoneIndex].rotation *= Quaternion.Slerp(Quaternion.Identity, morphBoneStruct.Rotation, computedWeight);
            //            bones[morphBoneStruct.BoneIndex].dynamicPosition += morphBoneStruct.Translation * computedWeight;
            //        }
            //    }
            //}

            for (int i = 0; i < bones.Count; i++)
            {
                IK(i, bones);
            }
            UpdateAppendBones();
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

            return charater;
        }


        Vector3 SafeNormalize(Vector3 vector3)
        {
            float dp3 = Math.Max(0.00001f, Vector3.Dot(vector3, vector3));
            return vector3 / MathF.Sqrt(dp3);
        }

        public class Bone
        {
            public Vector3 dynamicPosition;
            public Quaternion rotation;
            public Vector3 staticPosition;
            public Vector3 appendTranslation;
            public Quaternion appendRotation = Quaternion.Identity;
            public int index;

            public int CCDIterateLimit = 0;
            public float CCDAngleLimit = 0;

            public int ParentIndex = -1;
            public int IKTargetIndex = -1;
            public int AppendParentIndex = -1;
            public string Name;
            public string NameEN;
            public bool IsAppendRotation;
            public bool IsAppendTranslation;
            public bool IsPhysicsFreeBone;
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
                       Matrix4x4.CreateTranslation(staticPosition + appendTranslation + dynamicPosition) * list[ParentIndex]._generatedTransform;
                }
                else
                {
                    _generatedTransform = Matrix4x4.CreateTranslation(-staticPosition) *
                       Matrix4x4.CreateFromQuaternion(rotation * appendRotation) *
                       Matrix4x4.CreateTranslation(staticPosition + appendTranslation + dynamicPosition);
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
}

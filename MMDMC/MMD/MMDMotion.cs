using System;
using System.Collections.Generic;
using System.Numerics;

namespace MMDMC.MMD
{
    public class MMDMotion
    {
        public Dictionary<string, List<BoneKeyFrame>> BoneKeyFrameSet { get; set; } = new();
        public Dictionary<string, List<MorphKeyFrame>> MorphKeyFrameSet { get; set; } = new();
        public int lastFrame;
        const float c_framePerSecond = 30;
        public BoneKeyFrame GetBoneMotion(string key, float time)
        {
            if (!BoneKeyFrameSet.TryGetValue(key, out var keyframeSet))
            {
                return new BoneKeyFrame() { Rotation = Quaternion.Identity };
            }

            int left = 0;
            int right = keyframeSet.Count - 1;
            float frame = Math.Max(time * c_framePerSecond, 0);

            if (keyframeSet.Count == 1)
                return keyframeSet[left];
            if (keyframeSet[right].Frame < frame)
                return keyframeSet[right];

            while (right - left > 1)
            {
                int mid = (right + left) / 2;
                if (keyframeSet[mid].Frame > frame)
                    right = mid;
                else
                    left = mid;
            }
            return ComputeKeyFrame(keyframeSet[left], keyframeSet[right], frame);
        }

        public float GetMorphWeight(string key, float time)
        {
            if (!MorphKeyFrameSet.TryGetValue(key, out var keyframeSet))
            {
                return 0.0f;
            }
            int left = 0;
            int right = keyframeSet.Count - 1;
            float frame = Math.Max(time * c_framePerSecond, 0);

            if (keyframeSet.Count == 1)
                return keyframeSet[0].Weight;
            if (keyframeSet[right].Frame < frame)
                return keyframeSet[right].Weight;

            while (right - left > 1)
            {
                int mid = (right + left) / 2;
                if (keyframeSet[mid].Frame > frame)
                    right = mid;
                else
                    left = mid;
            }

            return ComputeKeyFrame(keyframeSet[left], keyframeSet[right], frame);
        }

        static BoneKeyFrame ComputeKeyFrame(in BoneKeyFrame _Left, in BoneKeyFrame _Right, float frame)
        {
            float t = (frame - _Left.Frame) / (_Right.Frame - _Left.Frame);
            float amountR = GetAmount(_Right.rInterpolator, t);
            float amountX = GetAmount(_Right.xInterpolator, t);
            float amountY = GetAmount(_Right.yInterpolator, t);
            float amountZ = GetAmount(_Right.zInterpolator, t);


            BoneKeyFrame newKeyFrame = new BoneKeyFrame();
            newKeyFrame.Frame = (int)MathF.Round(frame);
            newKeyFrame.rotation = Quaternion.Slerp(_Left.rotation, _Right.rotation, amountR);
            newKeyFrame.translation = Lerp(_Left.translation, _Right.translation, new Vector3(amountX, amountY, amountZ));

            return newKeyFrame;
        }
        static float ComputeKeyFrame(MorphKeyFrame _left, MorphKeyFrame _right, float frame)
        {
            float amount = (float)(frame - _left.Frame) / (_right.Frame - _left.Frame);
            return Lerp(_left.Weight, _right.Weight, amount);
        }
        static float Lerp(float x, float y, float s)
        {
            return x * (1 - s) + y * s;
        }
        static Vector3 Lerp(Vector3 x, Vector3 y, Vector3 s)
        {
            return x * (Vector3.One - s) + y * s;
        }

        static float GetAmount(Interpolator interpolator, float a)
        {
            if (interpolator.ax == interpolator.ay && interpolator.bx == interpolator.by)
                return a;
            var curve = Utility.CubicBezierCurve.Load(interpolator.ax, interpolator.ay, interpolator.bx, interpolator.by);
            return curve.Sample(a);
        }
    }
    public static partial class VMDFormatExtension
    {
        public static MMDMotion Load(VMDFormat vmd)
        {
            MMDMotion motion = new MMDMotion();

            foreach (var pair in vmd.BoneKeyFrameSet)
            {
                if (pair.Value.Count > 0)
                {
                    motion.BoneKeyFrameSet.Add(pair.Key, new List<BoneKeyFrame>(pair.Value));
                    motion.lastFrame = Math.Max(pair.Value[pair.Value.Count - 1].Frame, motion.lastFrame);
                }
            }
            foreach (var pair in vmd.MorphKeyFrameSet)
            {
                if (pair.Value.Count > 0)
                {
                    motion.MorphKeyFrameSet.Add(pair.Key, new List<MorphKeyFrame>(pair.Value));
                    motion.lastFrame = Math.Max(pair.Value[pair.Value.Count - 1].Frame, motion.lastFrame);
                }
            }
            return motion;
        }
    }
}

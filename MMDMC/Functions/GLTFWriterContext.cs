using MMDMC.GLTF;
using MMDMC.Utility;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace MMDMC.Functions
{
    public class GLTFWriterContext
    {
        public Dictionary<string, GLTFAccessor> accessorStart = new Dictionary<string, GLTFAccessor>();
        //public Dictionary<string, GLTFAccessor> accessorStart = new Dictionary<string, GLTFAccessor>();
        //public void StartWriteAccessorMark(string name)
        //{
        //    accessorStart.Add(name, accessors.Count);
        //}
        public List<GLTFAccessor> accessors = new List<GLTFAccessor>();
        public List<GLTFBufferView> bufferViews = new List<GLTFBufferView>();
        public Dictionary<string, GLTFBufferView> bufferViewStart = new Dictionary<string, GLTFBufferView>();
        public Dictionary<string, OffsetAndLength> bufferOffset = new Dictionary<string, OffsetAndLength>();
        public GLTFBuffer defaultBuffer = new GLTFBuffer();

        public int streamPosition = 0;

        public void MarkBuf(string name, GLTFAccessor desc1)
        {
            MarkBuf(name, new GLTFBufferView(), desc1);
        }

        public void MarkBuf(string name, GLTFBufferView desc, GLTFAccessor desc1)
        {
            int streamCurrentposition = (int)writer.BaseStream.Position;
            int length = streamCurrentposition - streamPosition;
            bufferOffset[name] = new OffsetAndLength(streamPosition, length, 0);

            bufferViewStart[name] = desc;
            desc.buffer = defaultBuffer;
            desc.byteLength = length;
            desc.byteOffset = streamPosition;
            desc.name = name;
            bufferViews.Add(desc);

            if (desc1 != null)
            {
                accessorStart[name] = desc1;
                desc1.bufferView = desc;
                desc1.byteOffset = 0;
                desc1.name = name;
                accessors.Add(desc1);
            }
            streamPosition = (int)writer.BaseStream.Position;
        }

        public GLTFAccessor CreateAccessor(string name, Vector4[] vector4s, int? target = 34962)
        {
            WriteVectors(vector4s, out var min, out var max);
            return CreateAccessor(name, new GLTFBufferView { target = target }, new GLTFAccessor { componentType = 5126, count = vector4s.Length, type = "VEC4", min = min, max = max });
        }

        public GLTFAccessor CreateAccessor(string name, Vector3[] vector3s, int? target = 34962)
        {
            WriteVectors(vector3s, out var min, out var max);
            return CreateAccessor(name, new GLTFBufferView { target = target }, new GLTFAccessor { componentType = 5126, count = vector3s.Length, type = "VEC3", min = min, max = max });
        }

        public GLTFAccessor CreateAccessor2(string name, Vector3[] vector3s)
        {
            WriteVectors(vector3s, out var min, out var max);
            var accessor = new GLTFAccessor { componentType = 5126, count = vector3s.Length, type = "VEC3", min = min, max = max };
            CreateAccessor(name, new GLTFBufferView { target = 34962 }, accessor);
            return accessor;
        }
        public GLTFAccessor CreateAccessor(string name, Vector2[] vector2s, int? target = 34962)
        {
            WriteVectors(vector2s, out var min, out var max);
            return CreateAccessor(name, new GLTFBufferView { target = target }, new GLTFAccessor { componentType = 5126, count = vector2s.Length, type = "VEC2", min = min, max = max });
        }
        public GLTFAccessor CreateAccessor(string name, float[] floats, int? target = 34962)
        {
            WriteFloats(floats, out var min, out var max);
            return CreateAccessor(name, new GLTFBufferView { target = target }, new GLTFAccessor { componentType = 5126, count = floats.Length, type = "SCALAR", min = min, max = max });
        }
        public GLTFAccessor CreateAccessor(string name, Matrix4x4[] matrix, int? target = 34962)
        {
            writer.Write(MemoryMarshal.AsBytes<Matrix4x4>(matrix));
            return CreateAccessor(name, new GLTFBufferView { target = target }, new GLTFAccessor { componentType = 5126, count = matrix.Length, type = "MAT4" });
        }
        public GLTFAccessor CreateAccessor(string name, int[] ints, int? target = 34963)
        {
            writer.Write(MemoryMarshal.AsBytes<int>(ints));
            return CreateAccessor(name, new GLTFBufferView { target = target }, new GLTFAccessor { componentType = 5125, count = ints.Length, type = "SCALAR" });
        }
        public GLTFAccessor CreateAccessor4(string name, int[] ints, int? target = 34963)
        {
            writer.Write(MemoryMarshal.AsBytes<int>(ints));
            return CreateAccessor(name, new GLTFBufferView { target = target }, new GLTFAccessor { componentType = 5125, count = ints.Length / 4, type = "VEC4" });
        }
        public GLTFAccessor CreateAccessor4(string name, ushort[] shorts, int? target = 34963)
        {
            writer.Write(MemoryMarshal.AsBytes<ushort>(shorts));
            return CreateAccessor(name, new GLTFBufferView { target = target }, new GLTFAccessor { componentType = 5123, count = shorts.Length / 4, type = "VEC4" });
        }

        public GLTFAccessor CreateAccessor(string name, GLTFAccessor desc1)
        {
            return CreateAccessor(name, new GLTFBufferView(), desc1);
        }

        public GLTFAccessor CreateAccessor(string name, GLTFBufferView desc, GLTFAccessor desc1)
        {
            int streamCurrentposition = (int)writer.BaseStream.Position;
            int length = streamCurrentposition - streamPosition;
            if (name != null)
            {
                bufferOffset[name] = new OffsetAndLength(streamPosition, length, 0);

                bufferViewStart[name] = desc;
            }
            desc.buffer = defaultBuffer;
            desc.byteLength = length;
            desc.byteOffset = streamPosition;
            desc.name = name;
            bufferViews.Add(desc);

            if (desc1 != null)
            {
                if (name != null)
                    accessorStart[name] = desc1;
                desc1.bufferView = desc;
                desc1.byteOffset = 0;
                desc1.name = name;
                accessors.Add(desc1);
            }
            streamPosition = (int)writer.BaseStream.Position;
            return desc1;
        }

        public void WriteVectors(Vector4[] vec4s, out float[] min, out float[] max)
        {
            min = new float[4];
            max = new float[4];

            min[0] = vec4s[0].X;
            min[1] = vec4s[0].Y;
            min[2] = vec4s[0].Z;
            min[3] = vec4s[0].W;
            max[0] = vec4s[0].X;
            max[1] = vec4s[0].Y;
            max[2] = vec4s[0].Z;
            max[3] = vec4s[0].W;

            for (int i = 0; i < vec4s.Length; i++)
            {
                min[0] = Math.Min(vec4s[i].X, min[0]);
                min[1] = Math.Min(vec4s[i].Y, min[1]);
                min[2] = Math.Min(vec4s[i].Z, min[2]);
                min[3] = Math.Min(vec4s[i].W, min[3]);
                max[0] = Math.Max(vec4s[i].X, max[0]);
                max[1] = Math.Max(vec4s[i].Y, max[1]);
                max[2] = Math.Max(vec4s[i].Z, max[2]);
                max[3] = Math.Max(vec4s[i].W, max[3]);
            }
            writer.Write(MemoryMarshal.AsBytes<Vector4>(vec4s));
        }

        public void WriteVectors(Vector3[] vec3s, out float[] min, out float[] max)
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

        public void WriteVectors(Vector2[] vec2s, out float[] min, out float[] max)
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

        public void WriteFloats(float[] values, out float[] min, out float[] max)
        {
            min = new float[1];
            max = new float[1];

            min[0] = values[0];
            max[0] = values[0];

            for (int i = 0; i < values.Length; i++)
            {
                min[0] = Math.Min(values[i], min[0]);
                max[0] = Math.Max(values[i], max[0]);
            }
            writer.Write(MemoryMarshal.AsBytes<float>(values));
        }

        public BinaryWriterPlus writer;

    }
    public struct OffsetAndLength
    {
        public int offset;
        public int length;
        public int localOffset;

        public OffsetAndLength(int offset, int length)
        {
            this.offset = offset;
            this.length = length;
            this.localOffset = 0;
        }

        public OffsetAndLength(int offset, int length, int localOffset)
        {
            this.offset = offset;
            this.length = length;
            this.localOffset = localOffset;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MMDMotionCompute.Utility
{
    public class BinaryWriterPlus : BinaryWriter
    {
        public BinaryWriterPlus (Stream stream) : base(stream)
        {

        }

        public void Write(Vector3 vec3)
        {
            Write(vec3.X);
            Write(vec3.Y);
            Write(vec3.Z);
        }

        public void Write(Quaternion quat)
        {
            Write(quat.X);
            Write(quat.Y);
            Write(quat.Z);
            Write(quat.W);
        }

        public void Write(Matrix4x4 mat4)
        {
            Write(mat4.M11);
            Write(mat4.M12);
            Write(mat4.M13);
            Write(mat4.M14);
            Write(mat4.M21);
            Write(mat4.M22);
            Write(mat4.M23);
            Write(mat4.M24);
            Write(mat4.M31);
            Write(mat4.M32);
            Write(mat4.M33);
            Write(mat4.M34);
            Write(mat4.M41);
            Write(mat4.M42);
            Write(mat4.M43);
            Write(mat4.M44);
        }
    }
}

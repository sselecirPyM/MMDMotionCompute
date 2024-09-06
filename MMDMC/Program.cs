using CommandLine;
using MMDMC.Functions;
using MMDMC.MMD;
using System;
using System.IO;

namespace MMDMC
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<ExportOptions>(args).WithParsed(Run);
        }

        static void Run(ExportOptions args)
        {
            PMXFormat pmx;
            VMDFormat vmd = null;

            pmx = PMXFormat.Load(new BinaryReader(File.OpenRead(args.model)));
            if (args.motion != null)
                vmd = VMDFormat.Load(new BinaryReader(File.OpenRead(args.motion)));

            GLTFUtil.SaveAsGLTF2(pmx, vmd, args, args.output);
        }
    }
}

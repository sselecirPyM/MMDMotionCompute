using System.Text.Json;

namespace MMDMC.GLTF
{
    public class GLTFModel
    {
        public GLTFAsset asset { get; set; } = new GLTFAsset();
        public GLTFAccessor[] accessors { get; set; }
        public int scene { get; set; }
        public GLTFScene[] scenes { get; set; }
        public GLTFBuffer[] buffers { get; set; }
        public GLTFBufferView[] bufferViews { get; set; }
        public GLTFMaterial[] materials { get; set; }
        public GLTFMesh[] meshes { get; set; }
        public GLTFNode[] nodes { get; set; }
        public GLTFSkin[] skins { get; set; }
        public GLTFImage[] images { get; set; }
        public GLTFTexture[] textures { get; set; }
        public GLTFAnimation[] animations { get; set; }

        public byte[] ToBytes()
        {
            MakeIndex(accessors);
            MakeIndex(buffers);
            MakeIndex(bufferViews);
            MakeIndex(materials);
            MakeIndex(meshes);
            MakeIndex(nodes);
            MakeIndex(skins);
            MakeIndex(images);
            MakeIndex(textures);

            if (animations != null)
                foreach (var animation in animations)
                {
                    MakeIndex(animation.samplers);
                }

            var option = new JsonSerializerOptions(JsonSerializerDefaults.General);
            option.WriteIndented = true;
            option.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(this, option);
            return data;
        }

        static void MakeIndex(IndexableObject[] objects)
        {
            for (int i = 0; i < objects.Length; i++)
                objects[i]._Index = i;
        }
    }
}

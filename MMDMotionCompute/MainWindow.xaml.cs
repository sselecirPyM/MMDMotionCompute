using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;
using MMDMotionCompute.FileFormat;
using Microsoft.Win32;
using System.IO;
using MMDMotionCompute.Functions;

namespace MMDMotionCompute
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public string filePath;
        public PMXFormat pmx;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "pmx|*.pmx|vmd|*.vmd";
            openFileDialog.Title = "open";
            if (openFileDialog.ShowDialog() == true)
            {
                pmx = PMXFormat.Load(new BinaryReader(File.OpenRead(openFileDialog.FileName)));
                imagePath = Path.GetDirectoryName(openFileDialog.FileName);
            }
        }
        string imagePath;

        private void Button_Export(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "gltf2|*.gltf";
            saveFileDialog.Title = "save";
            if (saveFileDialog.ShowDialog() == true)
            {
                GLTFUtil.SaveAsGLTF2(pmx, imagePath, saveFileDialog.FileName);
            }
        }
    }
}

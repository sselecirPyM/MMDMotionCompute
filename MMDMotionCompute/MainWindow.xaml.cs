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
using MMDMotionCompute.MMD;
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
        public VMDFormat vmd;

        private void Button_PMX(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "pmx|*.pmx";
            openFileDialog.Title = "open";
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    pmx = PMXFormat.Load(new BinaryReader(File.OpenRead(openFileDialog.FileName)));
                    showPath.Text = openFileDialog.FileName;
                    imagePath = Path.GetDirectoryName(openFileDialog.FileName);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.ToString());
                }
            }
        }

        private void Button_VMD(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "vmd|*.vmd";
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    vmd = VMDFormat.Load(new BinaryReader(File.OpenRead(openFileDialog.FileName)));
                    showVmdPath.Text = openFileDialog.FileName;
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.ToString());
                }
            }
        }
        string imagePath;

        public bool physics { get; set; } = true;
        public bool sparseMorph { get; set; } = true;
        public float exportScale { get; set; } = 0.08f;

        private void Button_Export(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "gltf2|*.gltf";
            saveFileDialog.Title = "save";
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    GLTFUtil.SaveAsGLTF2(pmx, vmd, new ExportOptions() { physics = physics, sparseMorph = sparseMorph, exportScale = exportScale }, saveFileDialog.FileName);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.ToString());
                }
            }
        }
    }
}

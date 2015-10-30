using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace WavConvToExtensible {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        private void Log(string s) {
            textBoxLog.AppendText(s);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            Log(string.Format("WavConvToExtensible {0}\n", AssemblyVersion));
        }

        private void buttonBrowseInputFile_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = Properties.Resources.FilterWavFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxInputFile.Text = dlg.FileName;
        }

        private void buttonBrowseOutputFile_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = Properties.Resources.FilterWavFiles;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxOutputFile.Text = dlg.FileName;
        }

        private void buttonStartConversion_Click(object sender, RoutedEventArgs e) {
            if (textBoxInputFile.Text.Length == 0) {
                MessageBox.Show(Properties.Resources.ErrSpecifyInputWavFile);
                return;
            }
            if (textBoxOutputFile.Text.Length == 0) {
                MessageBox.Show(Properties.Resources.ErrSpecifyOutputWavFile);
                return;
            }

            string pathRead = textBoxInputFile.Text;
            string pathWrite = textBoxOutputFile.Text;

            WavRWLib2.WavReader r = new WavRWLib2.WavReader();
            using (var br = new BinaryReader(File.Open(pathRead, FileMode.Open, FileAccess.Read, FileShare.Read))) {
                if (!r.ReadHeaderAndSamples(br, 0, -1)) {
                    string s = string.Format("エラー: ファイル読み込み失敗 {0}", pathRead);
                    MessageBox.Show(s);
                    Log(s + "\n");
                    return;
                }
            }

            Log(string.Format("WAVファイル読み込み。{0}\n    読み込んだファイルのfmt subchunkSize = {1}\n", pathRead, r.FmtSubChunkSize));

            WavRWLib2.WavWriter w = new WavRWLib2.WavWriter();

            string directory = System.IO.Path.GetDirectoryName(pathWrite);
            if (!System.IO.Directory.Exists(directory)) {
                System.IO.Directory.CreateDirectory(directory);
            }

            using (var bw = new BinaryWriter(File.Open(pathWrite, FileMode.Create, FileAccess.Write, FileShare.Write))) {
                w.Write(bw, r.NumChannels, r.BitsPerSample, r.ValidBitsPerSample, r.SampleRate, r.SampleValueRepresentationType, r.NumFrames, r.GetSampleArray());
            }
            Log(string.Format("WAVファイル書き込み終了。{0}\n", pathWrite));
        }

    }
}

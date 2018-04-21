using System.IO;
using System.Windows;
using Microsoft.Win32;
using PcmDataLib;

namespace AiffHeaderRead {
    public partial class MainWindow : Window {
        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            textBoxOutput.Text +=
                string.Format("AiffHeaderRead {0}", AssemblyVersion);


        }

        private void MainWindowDragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effects = DragDropEffects.Copy;
            } else {
                e.Effects = DragDropEffects.None;
            }
        }

        private void MainWindowDragDrop(object sender, DragEventArgs e) {
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (null == paths) {
                MessageBox.Show("何かがドロップされましたがファイルではないようでした。");
                return;
            }

            for (int i = 0; i < paths.Length; ++i) {
                var path = paths[i];
                textBoxInputFile.Text = path;
                Read(path);
            }
        }

        private void textBoxInputFile_PreviewDragOver(object sender, DragEventArgs e) {
            e.Handled = true;
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e) {
            var dlg = new OpenFileDialog();

            dlg.DefaultExt = ".aif";
            dlg.Filter = "AIFF Files (*.aiff,*.aifc;*.aif)|*.aiff;*.aifc;*.aif|All files(*.*)|*.*";
            var result = dlg.ShowDialog();
            if (result == true) {
                var path = dlg.FileName;
                textBoxInputFile.Text = path;
                Read(path);
            }
        }

        private void Read(string path) {
            textBoxOutput.Text =
                string.Format("AiffHeaderRead {0}\r\n", AssemblyVersion);

            var ar = new AiffHeaderReader();

            var pcm = new PcmData();

            textBoxOutput.Text +=
                string.Format("File size = {0} bytes\r\n", new System.IO.FileInfo(path).Length);

            string s = "";
            using (var br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read))) {
                s = ar.ReadHeader(br, out pcm);
            }

            textBoxOutput.Text += s;
        }

        private void buttonCopyToClipboard_Click(object sender, RoutedEventArgs e) {
            System.Windows.Clipboard.SetText(textBoxOutput.Text, TextDataFormat.UnicodeText);
        }
    }
}

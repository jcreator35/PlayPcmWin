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
using System.Windows.Shapes;
using System.IO;

namespace WavFormatConv
{
    /// <summary>
    /// Interaction logic for AddChunk.xaml
    /// </summary>
    public partial class AddChunkDialog : Window {
        public AddChunkDialog() {
            InitializeComponent();
        }
        private bool mIntitialized = false;

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mIntitialized = true;
            UpdateDialogStatus();

            LocalizeUI();
        }

        private void LocalizeUI() {
            buttonBext.Content = Properties.Resources.ButtonBext;
            buttonDS64.Content = Properties.Resources.ButtonDS64;
            buttonID3.Content = Properties.Resources.ButtonID3;
            buttonID3BrowseImage.Content = Properties.Resources.buttonID3BrowseImage;
            buttonJunk.Content = Properties.Resources.ButtonJunk;
            groupBoxBext.Header = Properties.Resources.GroupBoxBext;
            groupBoxDS64.Header = Properties.Resources.GroupBoxDS64;
            groupBoxID3.Header = Properties.Resources.GroupBoxID3;
            groupBoxJunk.Header = Properties.Resources.GroupBoxJunk;
            labelAlbumCoverArtFile.Content = Properties.Resources.LabelAlbumCoverArtFile;
        }

        private void UpdateDialogStatus() {
            if (!mIntitialized) {
                return;
            }

        }

        public WavChunkParams WavChunk { get; set; }

        private bool StringFormatCheck(string s, string name, int bytesMax) {
            var b = UTF7Encoding.ASCII.GetBytes(s);
            if (b == null) {
                MessageBox.Show(string.Format("Error: {0} text must be alphanumeric character", name));
                return false;
            }
            if (bytesMax < b.Length) {
                MessageBox.Show(string.Format("Error: {0} text must be equal to or shorter than {1} characters", name, bytesMax));
                return false;
            }
            return true;
        }

        private void buttonBext_Click(object sender, RoutedEventArgs e) {
            if (!StringFormatCheck(textBoxBextDescription.Text, "Description", 256)) {
                return;
            }
            if (!StringFormatCheck(textBoxBextOriginator.Text, "Originator", 32)) {
                return;
            }
            if (!StringFormatCheck(textBoxBextOriginatorReference.Text, "OriginatorReference", 32)) {
                return;
            }
            if (!StringFormatCheck(textBoxBextOriginationDate.Text, "OriginationDate", 10)) {
                return;
            }
            if (!StringFormatCheck(textBoxBextOriginationTime.Text, "OriginationTime", 8)) {
                return;
            }
            int timeReference;
            if (!Int32.TryParse(textBoxBextTimeReference.Text, out timeReference)) {
                MessageBox.Show("Error: TimeReference must be integer number");
                return;
            }

            var p = new BextChunkParams();
            p.Description = textBoxBextDescription.Text;
            p.Originator = textBoxBextOriginator.Text;
            p.OriginatorReference = textBoxBextOriginatorReference.Text;
            p.OriginationDate = textBoxBextOriginationDate.Text;
            p.OriginationTime = textBoxBextOriginationTime.Text;
            p.TimeReference = timeReference;

            WavChunk = p;

            DialogResult = true;
            Close();
        }

        private void buttonJunk_Click(object sender, RoutedEventArgs e) {
            int bytes;
            if (!Int32.TryParse(textboxJunkSize.Text, out bytes)) {
                MessageBox.Show("Error: JUNK chunk size parse error");
                return;
            }

            var p = new JunkChunkParams(bytes);

            WavChunk = p;

            DialogResult = true;
            Close();
        }

        private void buttonDS64_Click(object sender, RoutedEventArgs e) {
            var p = new DS64ChunkParams(-1, -1, -1);
            WavChunk = p;

            DialogResult = true;
            Close();
        }

        private void buttonID3_Click(object sender, RoutedEventArgs e) {
            string mimeType = "";
            byte[] albumCoverArt = new byte[0];
            string path = textBoxId3AlbumCoverArtPath.Text;
            if (path != null && 0 < path.Length) {
                using (var br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
                    albumCoverArt = br.ReadBytes((int)br.BaseStream.Length);
                }

                switch (System.IO.Path.GetExtension(path).ToLower()) {
                case ".png":
                    mimeType = "image/png";
                    break;
                case ".jpg":
                case ".jpeg":
                    mimeType = "image/jpeg";
                    break;
                default:
                    MessageBox.Show(
                        string.Format("Error: unrecognized image format. png or jpeg required: {0}", path));
                    return;
                }
            }

            var p = new ID3ChunkParams();
            p.Title = textBoxId3Title.Text;
            p.Album = textBoxId3Album.Text;
            p.Artists = textBoxId3Artists.Text;
            p.AlbumCoverArt = albumCoverArt;
            p.AlbumCoverArtMimeType = mimeType;

            if ((p.Title == null || p.Title.Length == 0) &&
                    (p.Album == null || p.Album.Length == 0) &&
                    (p.Artists == null || p.Artists.Length == 0)) {
                MessageBox.Show("Error: ID3 title or album or artist name is required to add ID3 chunk");
                return;
            }

            WavChunk = p;

            DialogResult = true;
            Close();
        }

        private void buttonID3BrowseImage_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Image files|*.jpg;*.jpeg;*.png|JPEG files(*.jpg,*.jpeg)|*.jpg;*.jpeg|PNG files(*.png)|*.png";
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxId3AlbumCoverArtPath.Text = dlg.FileName;
        }

    }

}

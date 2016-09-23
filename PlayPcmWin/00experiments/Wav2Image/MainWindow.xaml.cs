using System.Windows;
using System;
using System.IO;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Media;

namespace Wav2Image {
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private PcmDataLib.PcmData mPcmData;
        BackgroundWorker mFileReadWorker;

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mFileReadWorker = new BackgroundWorker();
            mFileReadWorker.DoWork += new DoWorkEventHandler(FileReadWorkerDoWork);
            mFileReadWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(FileReadWorkerRunWorkerCompleted);
            Title = string.Format("Wav2Image {0}", AssemblyVersion);
        }

        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        class FileReadWorkerDoArgs {
            public string path;

            public FileReadWorkerDoArgs(string path) {
                this.path = path;
            }
        };

        class FileReadWorkerCompletedArgs {
            public string path;
            public bool result;
            public string msg;

            public FileReadWorkerCompletedArgs(string path, bool result, string msg) {
                this.path = path;
                this.result = result;
                this.msg = msg;
            }
        };

        private void buttonBrowse_Click(object sender, RoutedEventArgs e) {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "WAVEファイル|*.WAV;*.WAVE|全てのファイル|*.*";
            {
                Nullable<bool> result = dlg.ShowDialog();
                if (result != true) {
                    return;
                }
            }

            mFileReadWorker.RunWorkerAsync(new FileReadWorkerDoArgs(dlg.FileName));
        }

        private void FileReadWorkerDoWork(object sender, DoWorkEventArgs e) {
            var args = e.Argument as FileReadWorkerDoArgs;

            e.Result = new FileReadWorkerCompletedArgs(args.path, true, "");

            var wavData = new WavRWLib2.WavData();

            try {
                using (var br = new BinaryReader(new FileStream(
                        args.path, System.IO.FileMode.Open))) {
                    bool r = wavData.ReadStreamBegin(br, out mPcmData);
                    if (!r) {
                        e.Result = new FileReadWorkerCompletedArgs(args.path, false, "ファイル読み込み失敗 : " + args.path);
                        return;
                    }

                    mPcmData.SetSampleArray(wavData.ReadStreamReadOne(br, 0x7fffffff));
                    wavData.ReadStreamEnd();
                }
            } catch (System.Exception ex) {
                e.Result = new FileReadWorkerCompletedArgs(args.path, false, ex.ToString());
            }
        }

        private void FileReadWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            var args = e.Result as FileReadWorkerCompletedArgs;
            if (!args.result) {
                MessageBox.Show(args.msg);
                mPcmData = null;
                textBoxReadPath.Text = "";
                // エラーでもUIを更新する
            }
            
            comboBoxCh.Items.Clear();
            for (int i=0; i < mPcmData.NumChannels; ++i) {
                comboBoxCh.Items.Add(string.Format("Ch {0}", i));
            }
            comboBoxCh.SelectedIndex = 0;

            slider1.Maximum = mPcmData.NumFrames;
            slider1.Minimum = 0;
            slider1.Value = 0;

            textBoxReadPath.Text = args.path;

            UpdateUI();
        }

        private void UpdateUI() {
            canvas1.Children.Clear();

            if (mPcmData == null) {
                return;
            }

            int ch = comboBoxCh.SelectedIndex;
            if (ch < 0) {
                ch = 0;
            }

            int sampleWidth = 2;
            if (true == radioButton1.IsChecked) {
                sampleWidth = 1;
            }
            if (true == radioButton2.IsChecked) {
                sampleWidth = 2;
            }
            if (true == radioButton4.IsChecked) {
                sampleWidth = 4;
            }
            if (true == radioButton8.IsChecked) {
                sampleWidth = 8;
            }

            for (int i=0; i < canvas1.ActualWidth / sampleWidth; ++i) {
                byte gray = 0;
                {
                    double sampleValue = mPcmData.GetSampleValueInDouble(ch, (long)(i+slider1.Value));
                    int iGray = (int)(128 * (sampleValue + 1.0));
                    if (255 < iGray) {
                        iGray = 255;
                    }
                    if (iGray < 0) {
                        iGray = 0;
                    }
                    gray = (byte)iGray;
                }
                var rect = new System.Windows.Shapes.Rectangle();
                rect.Fill = new SolidColorBrush(Color.FromRgb(gray, gray, gray));
                rect.Height = canvas1.ActualHeight;
                rect.Width = sampleWidth;
                Canvas.SetTop(rect, 0);
                Canvas.SetLeft(rect, i * sampleWidth);
                canvas1.Children.Add(rect);
            }
        }

        private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            UpdateUI();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) {
            UpdateUI();
        }

        private void comboBoxCh_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UpdateUI();
        }

        private void radioButton_Click(object sender, RoutedEventArgs e) {
            UpdateUI();
        }
    }
}

// 日本語。
using System;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WWKeyClassifier2 {
    public partial class MainWindow : Window {
        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }
        
        public MainWindow() {
            InitializeComponent();
            Title = string.Format("WWKeyClassifier2 version {0}", AssemblyVersion);
        }

        private enum State {
            NotReady,
            Ready,
            Processing,
        }

        private State mState = State.Ready;
        private BackgroundWorker mBw = new BackgroundWorker();
        private KeyClassifier mKeyClassifier;
        private bool mInitialized = false;

        private void Update() {
            if (!mInitialized) {
                return;
            }

            switch (mState) {
            case State.NotReady:
                mButtonStart.IsEnabled = false;
                mGroupBoxSettings.IsEnabled = true;
                break;
            case State.Ready:
                mButtonStart.IsEnabled = true;
                mGroupBoxSettings.IsEnabled = true;
                break;
            case State.Processing:
                mButtonStart.IsEnabled = false;
                mGroupBoxSettings.IsEnabled = false;
                break;
            }
        }

        private void mButtonStart_Click(object sender, RoutedEventArgs e) {
            if (!System.IO.File.Exists(mTextBoxInput.Text)) {
                MessageBox.Show(string.Format("Error: Input file does not exist. {0}", mTextBoxInput.Text));
                return;
            }

            if (0 != ".LRC".CompareTo(System.IO.Path.GetExtension(mTextBoxOutput.Text).ToUpperInvariant())) {
                MessageBox.Show(string.Format("Error: Output file extension should be .LRC. {0}", mTextBoxOutput.Text));
                return;
            }

            if (System.IO.File.Exists(mTextBoxOutput.Text)) {
                var mbr = MessageBox.Show(string.Format("Overwrite LRC file ? {0}", mTextBoxOutput.Text), "Overwrite confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (mbr != MessageBoxResult.Yes) {
                    // キャンセル。
                    return;
                }
            }

            mState = State.Processing;
            Update();

            KeyClassifier.PitchEnum pitchEnum = KeyClassifier.PitchEnum.ConcertPitch;
            if (mRadioButtonBaroquePitch.IsChecked == true) {
                pitchEnum = KeyClassifier.PitchEnum.BaroquePitch;
            }

            mBw.RunWorkerAsync(new WorkerParams(mTextBoxInput.Text, mTextBoxOutput.Text, pitchEnum));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mKeyClassifier = new KeyClassifier();

            mTextBoxLog.Text = "Ready.\n";

            mBw.DoWork += new DoWorkEventHandler(mBw_DoWork);
            mBw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mBw_RunWorkerCompleted);
            mBw.WorkerReportsProgress = true;
            mBw.ProgressChanged += new ProgressChangedEventHandler(mBw_ProgressChanged);
            mInitialized = true;
        }

        void mBw_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            string msg = e.UserState as string;
            if (0 < msg.Length) {
                mTextBoxLog.Text += msg;
                mTextBoxLog.ScrollToEnd();
            }

            mProgressBar.Value = e.ProgressPercentage;
        }

        class WorkerCompleteParams {
            public string ercd;
            public string writePath;
            public WorkerCompleteParams(string aErcd, string aWritePath) {
                ercd = aErcd;
                writePath = aWritePath;
            }
        };

        void mBw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            var wcp = e.Result as WorkerCompleteParams;

            if (wcp.ercd.Length != 0) {
                // エラー。
                mTextBoxLog.Text += string.Format("{0}\n", wcp.ercd);
                mTextBoxLog.ScrollToEnd();
            } else {
                // 成功。
                mTextBoxLog.Text += string.Format("Wrote {0}.\n", wcp.writePath);
                mTextBoxLog.ScrollToEnd();
            }

            mProgressBar.Value = 0;

            mState = State.Ready;
            Update();
        }

        class WorkerParams {
            public string inputAudioPath;
            public string outputLrcPath;
            public KeyClassifier.PitchEnum pitchEnum;
            public WorkerParams(string aInput, string aOutput, KeyClassifier.PitchEnum aPitchEnum) {
                inputAudioPath = aInput;
                outputLrcPath = aOutput;
                pitchEnum = aPitchEnum;
            }
        };

        void mBw_DoWork(object sender, DoWorkEventArgs e) {
            var param = e.Argument as WorkerParams;

            string ercd = mKeyClassifier.Classify(param.inputAudioPath, param.outputLrcPath, param.pitchEnum, mBw);
            e.Result = new WorkerCompleteParams(ercd, param.outputLrcPath);
        }

        private void mButtonBrowseInput_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = Properties.Resources.FilterReadAudioFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            mTextBoxInput.Text = dlg.FileName;
            InputFormUpdated();
        }

        private void Window_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effects = DragDropEffects.Copy;
            } else {
                e.Effects = DragDropEffects.None;
            }
        }

        private void TextInput_Drop(object sender, DragEventArgs e) {
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (null == paths) {
                var sb = new StringBuilder(Properties.Resources.DroppedDataIsNotFile);

                var formats = e.Data.GetFormats(false);
                foreach (var format in formats) {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "{1}    {0}", format, Environment.NewLine));
                }
                MessageBox.Show(sb.ToString());
                return;
            }
            mTextBoxInput.Text = paths[0];
            InputFormUpdated();
        }

        private void textBox_PreviewDragOver(object sender, DragEventArgs e) {
            e.Handled = true;
        }
        
        private void InputFormUpdated() {
            if (!mInitialized) {
                return;
            }
            
            if (0 < mTextBoxInput.Text.Length &&
                    0 < mTextBoxOutput.Text.Length) {
                mState = State.Ready;
            } else {
                mState = State.NotReady;
            }

            Update();
        }

        private void mTextBoxInput_TextChanged(object sender, TextChangedEventArgs e) {
            InputFormUpdated();
        }

        private void mTextBoxOutput_TextChanged(object sender, TextChangedEventArgs e) {
            InputFormUpdated();
        }

        private void mTextBoxOutput_Drop(object sender, DragEventArgs e) {
            var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (null == paths) {
                var sb = new StringBuilder(Properties.Resources.DroppedDataIsNotFile);

                var formats = e.Data.GetFormats(false);
                foreach (var format in formats) {
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "{1}    {0}", format, Environment.NewLine));
                }
                MessageBox.Show(sb.ToString());
                return;
            }
            mTextBoxOutput.Text = paths[0];
            InputFormUpdated();
        }

    }
}

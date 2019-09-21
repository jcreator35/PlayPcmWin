// 日本語。

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using WWAudioFilterCore;
using System.IO;

namespace WWAudioFilter {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window, IDisposable {
        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        BackgroundWorker mBackgroundWorker;
        Stopwatch mStopwatch = new Stopwatch();

        private bool mInitialized = false;

        private enum State {
            NotReady,
            Ready,
            ReadFile,
            Converting,
            WriteFile
        }

        private State mState = State.NotReady;

        private List<FilterBase> mFilters = new List<FilterBase>();

        private void ProcessCommandline() {
            var commandLine = new WWAudioFilterCommandLine();
            if (commandLine.ParseCommandLine()) {
                Application.Current.Shutdown();
                return;
            }
        }

        public MainWindow() {
            InitializeComponent();

            SetLocalizedTextToUI();
            Title = string.Format(CultureInfo.CurrentCulture, "WWAudioFilter version {0}", AssemblyVersion);

            mBackgroundWorker = new BackgroundWorker();
            mBackgroundWorker.WorkerReportsProgress = true;
            mBackgroundWorker.DoWork += new DoWorkEventHandler(Background_DoWork);
            mBackgroundWorker.ProgressChanged += new ProgressChangedEventHandler(Background_ProgressChanged);
            mBackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Background_RunWorkerCompleted);

#if false
            ProcessCommandline();
#endif
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (disposing) {
                // dispose managed resources
            }
            // free native resources
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
#if true
            ProcessCommandline();
#endif

            mInitialized = true;

            LoadSettings();

            Update();
        }

        private void LoadSettings() {
            cbEnableDither.IsChecked = Properties.Settings.Default.Dither;
            switch (Properties.Settings.Default.OutputBitDepth) {
            case 0:
                comboBoxOutputPcmFormat.SelectedIndex = (int)WWAFUtil.AFSampleFormat.Auto;
                break;
            case 16:
                comboBoxOutputPcmFormat.SelectedIndex = (int)WWAFUtil.AFSampleFormat.PcmInt16;
                break;
            case 24:
                comboBoxOutputPcmFormat.SelectedIndex = (int)WWAFUtil.AFSampleFormat.PcmInt24;
                break;
            case 32:
                if (Properties.Settings.Default.OutputFloat) {
                    comboBoxOutputPcmFormat.SelectedIndex = (int)WWAFUtil.AFSampleFormat.PcmFloat32;
                } else {
                    comboBoxOutputPcmFormat.SelectedIndex = (int)WWAFUtil.AFSampleFormat.PcmInt32;
                }
                break;
            case 64:
                if (Properties.Settings.Default.OutputFloat) {
                    comboBoxOutputPcmFormat.SelectedIndex = (int)WWAFUtil.AFSampleFormat.PcmFloat64;
                } else {
                    comboBoxOutputPcmFormat.SelectedIndex = (int)WWAFUtil.AFSampleFormat.PcmInt64;
                }
                break;
            default:
                Console.WriteLine("E: unknown bitdepth");
                comboBoxOutputPcmFormat.SelectedIndex = (int)WWAFUtil.AFSampleFormat.Auto;
                break;
            }
        }

        private void SaveSettings() {
            Properties.Settings.Default.Dither = cbEnableDither.IsChecked == true;
            var sf = (WWAFUtil.AFSampleFormat)comboBoxOutputPcmFormat.SelectedIndex;

            Properties.Settings.Default.OutputFloat = false;
            switch (sf) {
            case WWAFUtil.AFSampleFormat.Auto:
                Properties.Settings.Default.OutputBitDepth = 0;
                break;
            case WWAFUtil.AFSampleFormat.PcmInt16:
                Properties.Settings.Default.OutputBitDepth = 16;
                break;
            case WWAFUtil.AFSampleFormat.PcmInt24:
                Properties.Settings.Default.OutputBitDepth = 24;
                break;
            case WWAFUtil.AFSampleFormat.PcmInt32:
                Properties.Settings.Default.OutputBitDepth = 32;
                break;
            case WWAFUtil.AFSampleFormat.PcmFloat32:
                Properties.Settings.Default.OutputBitDepth = 32;
                Properties.Settings.Default.OutputFloat = true;
                break;
            case WWAFUtil.AFSampleFormat.PcmInt64:
                Properties.Settings.Default.OutputBitDepth = 64;
                break;
            case WWAFUtil.AFSampleFormat.PcmFloat64:
                Properties.Settings.Default.OutputBitDepth = 64;
                Properties.Settings.Default.OutputFloat = true;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            Properties.Settings.Default.Save();
        }

        private void SetLocalizedTextToUI() {
            buttonFilterAdd.Content = Properties.Resources.ButtonAddNewFilter;
            buttonBrowseInputFile.Content = Properties.Resources.ButtonBrowseB;
            buttonBrowseOutputFile.Content = Properties.Resources.ButtonBrowseR;
            buttonFilterEdit.Content = Properties.Resources.ButtonEditSelected;
            buttonFilterDelete.Content = Properties.Resources.ButtonDeleteSelected;
            buttonFilterLoad.Content = Properties.Resources.ButtonLoadSettings;
            buttonFilterDown.Content = Properties.Resources.ButtonMoveDownSelected;
            buttonFilterUp.Content = Properties.Resources.ButtonMoveUpSelected;
            buttonFilterSaveAs.Content = Properties.Resources.ButtonSaveSettingsAs;
            buttonStartConversion.Content = Properties.Resources.ButtonStartConversion;
            groupBoxFilterSettings.Header = Properties.Resources.GroupFilterSettings;
            groupBoxLog.Header = Properties.Resources.GroupLog;
            groupBoxOutputFile.Header = Properties.Resources.GroupOutputFile;
            groupBoxInputFile.Header = Properties.Resources.GroupInputFile;
            labelInputFile.Content = Properties.Resources.LabelInputFile;
            labelOutputFile.Content = Properties.Resources.LabelOutputFile;
        }

        private void Update() {
            if (!mInitialized) {
                return;
            }

            UpdateFilterSettings();

            switch (mState) {
            case State.NotReady:
                buttonStartConversion.IsEnabled = false;
                break;
            case State.Ready:
                buttonStartConversion.IsEnabled = true;
                break;
            case State.ReadFile:
            case State.Converting:
            case State.WriteFile:
                buttonStartConversion.IsEnabled = false;
                break;
            }
        }

        private void UpdateFilterButtons() {
            switch (mState) {
            case State.NotReady:
            case State.Ready:
                groupBoxFilterSettings.IsEnabled = true;
                if (listBoxFilters.SelectedIndex < 0) {
                    buttonFilterAdd.IsEnabled = true;
                    buttonFilterDelete.IsEnabled = false;
                    buttonFilterEdit.IsEnabled = false;
                    buttonFilterLoad.IsEnabled = true;
                    buttonFilterSaveAs.IsEnabled = false;

                    buttonFilterDown.IsEnabled = false;
                    buttonFilterUp.IsEnabled = false;
                } else {
                    buttonFilterAdd.IsEnabled = true;
                    buttonFilterDelete.IsEnabled = true;
                    buttonFilterEdit.IsEnabled = true;
                    buttonFilterLoad.IsEnabled = true;
                    buttonFilterSaveAs.IsEnabled = true;

                    buttonFilterDown.IsEnabled = listBoxFilters.SelectedIndex != listBoxFilters.Items.Count - 1;
                    buttonFilterUp.IsEnabled = listBoxFilters.SelectedIndex != 0;
                }
                break;
            case State.ReadFile:
            case State.Converting:
            case State.WriteFile:
                groupBoxFilterSettings.IsEnabled = false;
                break;
            }
        }

        private void UpdateFilterSettings() {
            int selectedIdx = listBoxFilters.SelectedIndex;

            listBoxFilters.Items.Clear();
            foreach (var f in mFilters) {
                listBoxFilters.Items.Add(f.ToDescriptionText());
            }

            if (listBoxFilters.Items.Count == 1) {
                // 最初に項目が追加された
                selectedIdx = 0;
            }
            if (0 <= selectedIdx && listBoxFilters.Items.Count <= selectedIdx) {
                // 選択されていた最後の項目が削除された。
                selectedIdx = listBoxFilters.Items.Count - 1;
            }
            listBoxFilters.SelectedIndex = selectedIdx;

            UpdateFilterButtons();
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        class RunWorkerArgs {
            public string FromPath { get; set; }
            public string ToPath { get; set; }

            public WWAFUtil.AFSampleFormat SampleFormat { get; set; }

            public bool Dither { get; set; }

            public RunWorkerArgs(string fromPath, string toPath, WWAFUtil.AFSampleFormat sf, bool bDither) {
                FromPath = fromPath;
                ToPath = toPath;
                SampleFormat = sf;
                Dither = bDither;
            }
        };

        void ProgressReportCallback(int percentage, WWAudioFilterCore.AudioFilterCore.ProgressArgs args) {
            mBackgroundWorker.ReportProgress(percentage, args);
        }

        void Background_DoWork(object sender, DoWorkEventArgs e) {
            var args = e.Argument as RunWorkerArgs;
            int rv = 0;

            var af = new WWAudioFilterCore.AudioFilterCore();
            try {
                rv = af.Run(args.FromPath, mFilters, args.ToPath, args.SampleFormat, args.Dither, ProgressReportCallback);
                if (rv < 0) {
                    e.Result = rv;
                    return;
                }
            } catch (Exception ex) {
                Console.WriteLine(ex);
                mBackgroundWorker.ReportProgress(100, new WWAudioFilterCore.AudioFilterCore.ProgressArgs(ex.ToString(), rv));
                e.Result = -1;
                return;
            }

            mBackgroundWorker.ReportProgress(100, new WWAudioFilterCore.AudioFilterCore.ProgressArgs("", rv));
            e.Result = rv;
        }

        void Background_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            var args = e.UserState as WWAudioFilterCore.AudioFilterCore.ProgressArgs;

            if (0 <= e.ProgressPercentage) {
                progressBar1.Value = e.ProgressPercentage;
            }

            if (0 < args.Message.Length) {
                textBoxLog.Text += args.Message;
                textBoxLog.ScrollToEnd();
            }
        }

        void Background_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            int rv = (int)e.Result;

            progressBar1.IsEnabled = false;
            progressBar1.Value = 0;

            groupBoxInputFile.IsEnabled = true;
            groupBoxFilterSettings.IsEnabled = true;
            groupBoxOutputFile.IsEnabled = true;
            buttonStartConversion.IsEnabled = true;

            mStopwatch.Stop();

            if (rv < 0) {
                var s = string.Format(CultureInfo.CurrentCulture, "{0} {1} {2}\r\n", Properties.Resources.Error, rv, WWFlacRWCS.FlacRW.ErrorCodeToStr(rv));
                MessageBox.Show(s, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Error);

                textBoxLog.Text += s;
                textBoxLog.ScrollToEnd();
            } else {
                textBoxLog.Text += string.Format(CultureInfo.CurrentCulture, Properties.Resources.LogCompleted, mStopwatch.Elapsed);
                textBoxLog.ScrollToEnd();
            }
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        private void buttonFilterAdd_Click(object sender, RoutedEventArgs e) {
            var w = new FilterConfiguration(null);
            w.ShowDialog();

            if (true == w.DialogResult) {
                mFilters.Add(w.Filter);
                Update();
                listBoxFilters.SelectedIndex = listBoxFilters.Items.Count - 1;
            }
        }

        private void buttonFilterEdit_Click(object sender, RoutedEventArgs e) {
            System.Diagnostics.Debug.Assert(0 <= listBoxFilters.SelectedIndex);
            System.Diagnostics.Debug.Assert(listBoxFilters.SelectedIndex < mFilters.Count);

            var w = new FilterConfiguration(mFilters[listBoxFilters.SelectedIndex]);
            w.ShowDialog();

            if (true == w.DialogResult) {
                int idx = listBoxFilters.SelectedIndex;
                mFilters.RemoveAt(idx);
                mFilters.Insert(idx, w.Filter);
                Update();
            }
        }

        private void buttonFilterUp_Click(object sender, RoutedEventArgs e) {
            int pos = listBoxFilters.SelectedIndex;
            var tmp = mFilters[pos];
            mFilters.RemoveAt(pos);
            mFilters.Insert(pos - 1, tmp);

            --listBoxFilters.SelectedIndex;

            Update();
        }

        private void buttonFilterDown_Click(object sender, RoutedEventArgs e) {
            int pos = listBoxFilters.SelectedIndex;
            var tmp = mFilters[pos];
            mFilters.RemoveAt(pos);
            mFilters.Insert(pos + 1, tmp);

            ++listBoxFilters.SelectedIndex;

            Update();
        }

        private void buttonFilterDelete_Click(object sender, RoutedEventArgs e) {
            mFilters.RemoveAt(listBoxFilters.SelectedIndex);

            Update();
        }

        private void listBoxFilters_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            UpdateFilterButtons();
        }

        private void buttonFilterSaveAs_Click(object sender, RoutedEventArgs e) {
            if (mFilters.Count() == 0) {
                MessageBox.Show(Properties.Resources.NothingToStore);
                return;
            }

            System.Diagnostics.Debug.Assert(0 < mFilters.Count());

            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = Properties.Resources.FilterWWAFilterFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            WWAudioFilterCore.AudioFilterCore.SaveFilteresToFile(mFilters, dlg.FileName);
        }

        private void buttonFilterLoad_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = Properties.Resources.FilterWWAFilterFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            var filters = WWAudioFilterCore.AudioFilterCore.LoadFiltersFromFile(dlg.FileName);
            if (filters == null) {
                return;
            }

            mFilters = filters;
            Update();
        }

        private void FilenameTextBoxUpdated() {
            if (0 < textBoxInputFile.Text.Length &&
                    0 < textBoxOutputFile.Text.Length) {
                mState = State.Ready;
            } else {
                mState = State.NotReady;
            }

            Update();
        }

        private void buttonBrowseInputFile_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = Properties.Resources.FilterReadAudioFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxInputFile.Text = dlg.FileName;
            FilenameTextBoxUpdated();
        }

        private enum FilterWriteAudioFilesType {
            FLAC = 1, //< FilterIndex is 1-based!! see document.
            DSF,
            WAV,
        };

        private void buttonBrowseOutputFile_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = Properties.Resources.FilterWriteAudioFiles;
            dlg.ValidateNames = true;

            var outputSampleFormat = (WWAFUtil.AFSampleFormat)comboBoxOutputPcmFormat.SelectedIndex;
            switch (outputSampleFormat) {
            case WWAFUtil.AFSampleFormat.Auto:
            case WWAFUtil.AFSampleFormat.PcmInt16:
            case WWAFUtil.AFSampleFormat.PcmInt24:
                dlg.FilterIndex = (int)FilterWriteAudioFilesType.FLAC;
                break;
            case WWAFUtil.AFSampleFormat.PcmInt32:
            case WWAFUtil.AFSampleFormat.PcmFloat32:
            case WWAFUtil.AFSampleFormat.PcmInt64:
            case WWAFUtil.AFSampleFormat.PcmFloat64:
                dlg.FilterIndex = (int)FilterWriteAudioFilesType.WAV;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxOutputFile.Text = dlg.FileName;
            FilenameTextBoxUpdated();
        }

        private void buttonStartConversion_Click(object sender, RoutedEventArgs e) {
            if (0 == string.Compare(textBoxInputFile.Text, textBoxOutputFile.Text, StringComparison.Ordinal)) {
                MessageBox.Show(Properties.Resources.ErrorWriteToReadFile, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }
            if (0 == mFilters.Count) {
                MessageBox.Show(Properties.Resources.ErrorFilterEmpty,
                    Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }

            bool bDither = cbEnableDither.IsChecked == true;

            var outputFileFormat = WWAFUtil.FileNameToFileFormatType(textBoxOutputFile.Text);

            var outputSampleFormat = (WWAFUtil.AFSampleFormat)comboBoxOutputPcmFormat.SelectedIndex;
            if (outputSampleFormat != WWAFUtil.AFSampleFormat.Auto) {
                bool formatMatched = true;
                switch (outputFileFormat) {
                case WWAFUtil.FileFormatType.FLAC:
                    if (outputSampleFormat == WWAFUtil.AFSampleFormat.PcmInt16 ||
                        outputSampleFormat == WWAFUtil.AFSampleFormat.PcmInt24) {
                        // OK
                    } else {
                        formatMatched = false;
                    }
                    break;
                case WWAFUtil.FileFormatType.WAVE:
                    // 全てOK
                    break;
                case WWAFUtil.FileFormatType.DSF:
                    if (outputSampleFormat == WWAFUtil.AFSampleFormat.Auto) {
                        // OK
                    } else {
                        formatMatched = false;
                    }
                    break;
                default:
                    break;
                }
                if (!formatMatched) {
                    MessageBox.Show(Properties.Resources.ErrorWriteFormatMismatch, Properties.Resources.Error, MessageBoxButton.OK, MessageBoxImage.Hand);
                    return;
                }
            }

            textBoxLog.Text = string.Empty;
            textBoxLog.Text += string.Format(CultureInfo.CurrentCulture, Properties.Resources.LogFileReadStarted, textBoxInputFile.Text);
            progressBar1.Value = 0;
            progressBar1.IsEnabled = true;

            groupBoxInputFile.IsEnabled = false;
            groupBoxFilterSettings.IsEnabled = false;
            groupBoxOutputFile.IsEnabled = false;
            buttonStartConversion.IsEnabled = false;

            mStopwatch.Reset();
            mStopwatch.Start();
            mBackgroundWorker.RunWorkerAsync(new RunWorkerArgs(textBoxInputFile.Text, textBoxOutputFile.Text, outputSampleFormat, bDither));
        }

        private void textBoxFile_PreviewDragOver(object sender, DragEventArgs e) {
            e.Handled = true;
        }

        private void textBoxFile_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effects = DragDropEffects.Copy;
            } else {
                e.Effects = DragDropEffects.None;
            }
        }

        private void textBoxInputFile_Drop(object sender, DragEventArgs e) {
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
            textBoxInputFile.Text = paths[0];
            FilenameTextBoxUpdated();
        }

        private void textBoxOutputFile_Drop(object sender, DragEventArgs e) {
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
            textBoxOutputFile.Text = paths[0];
            FilenameTextBoxUpdated();
        }

        private void textBoxOutputFile_TextChanged(object sender, TextChangedEventArgs e) {
            FilenameTextBoxUpdated();
        }

        private void textBoxInputFile_TextChanged(object sender, TextChangedEventArgs e) {
            FilenameTextBoxUpdated();
        }

        private void Window_Closing(object sender, CancelEventArgs e) {
            SaveSettings();
        }
    }
}

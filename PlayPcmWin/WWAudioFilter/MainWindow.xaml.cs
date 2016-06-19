using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Globalization;
using System.Text;
using System.Diagnostics;

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

        public MainWindow() {
            InitializeComponent();

            SetLocalizedTextToUI();
            Title = string.Format(CultureInfo.CurrentCulture, "WWAudioFilter version {0}", AssemblyVersion);

            mBackgroundWorker = new BackgroundWorker();
            mBackgroundWorker.WorkerReportsProgress = true;
            mBackgroundWorker.DoWork += new DoWorkEventHandler(Background_DoWork);
            mBackgroundWorker.ProgressChanged += new ProgressChangedEventHandler(Background_ProgressChanged);
            mBackgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Background_RunWorkerCompleted);

            var commandLine = new WWAudioFilterCommandLine();
            if (commandLine.ParseCommandLine()) {
                Application.Current.Shutdown();
                return;
            }
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
            mInitialized = true;
            Update();
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

        ////////////////////////////////////////////////////////////////////////////////////////

        class RunWorkerArgs {
            public string FromPath { get; set; }
            public string ToPath { get; set; }

            public RunWorkerArgs(string fromPath, string toPath) {
                FromPath = fromPath;
                ToPath = toPath;
            }
        };

        void ProgressReportCallback(int percentage, WWAudioFilterCore.ProgressArgs args) {
            mBackgroundWorker.ReportProgress(percentage, args);
        }

        void Background_DoWork(object sender, DoWorkEventArgs e) {
            var args = e.Argument as RunWorkerArgs;

            var af = new WWAudioFilterCore();

            int rv = af.Run(args.FromPath, mFilters, args.ToPath, ProgressReportCallback);
            if (rv < 0) {
                e.Result = rv;
                return;
            }

            mBackgroundWorker.ReportProgress(100, new WWAudioFilterCore.ProgressArgs("", rv));
            e.Result = rv;
        }

        void Background_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            var args = e.UserState as WWAudioFilterCore.ProgressArgs;

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

        ////////////////////////////////////////////////////////////////////////////////////////////////////

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

            WWAudioFilterCore.SaveFilteresToFile(mFilters, dlg.FileName);
        }

        private void buttonFilterLoad_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = Properties.Resources.FilterWWAFilterFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            var filters = WWAudioFilterCore.LoadFiltersFromFile(dlg.FileName);
            if (filters == null) {
                return;
            }

            mFilters = filters;
            Update();
        }

        private void InputFormUpdated() {
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
            dlg.Filter = Properties.Resources.FilterPcmFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxInputFile.Text = dlg.FileName;
            InputFormUpdated();
        }

        private void buttonBrowseOutputFile_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = Properties.Resources.FilterWriteAudioFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxOutputFile.Text = dlg.FileName;
            InputFormUpdated();
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
            mBackgroundWorker.RunWorkerAsync(new RunWorkerArgs(textBoxInputFile.Text, textBoxOutputFile.Text));
        }

        private void Window_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effects = DragDropEffects.Copy;
            } else {
                e.Effects = DragDropEffects.None;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e) {
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
            InputFormUpdated();
        }
    }
}

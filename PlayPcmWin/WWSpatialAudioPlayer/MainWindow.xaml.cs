// 日本語。

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using WWSpatialAudioUserCs;

namespace WWSpatialAudioPlayer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable {
        private ObservableCollection<VirtualSpeakerMotionType> mVirtualSpeakerTypeDisplayNames;
        private ObservableCollection<VirtualSpeakerProperty> mVirtualSpeakerProperties;
        private SpatialAudioPlayer mPlayer = new SpatialAudioPlayer();
        private BackgroundWorker mBwLoad = new BackgroundWorker();
        /// <summary>
        /// ログの表示行数。
        /// </summary>
        private const int LOG_LINE_NUM = 100;

        private List<string> mLogList = new List<string>();

        public MainWindow() {
            mVirtualSpeakerTypeDisplayNames = new ObservableCollection<VirtualSpeakerMotionType>() { VirtualSpeakerMotionType.Static, VirtualSpeakerMotionType.UserSpecifiedPosition, VirtualSpeakerMotionType.RotateAroundYourHead };
            mVirtualSpeakerProperties = new ObservableCollection<VirtualSpeakerProperty>();
            //mVirtualSpeakerProperties.Add(new VirtualSpeakerProperty(0, "FrontLeft", VirtualSpeakerMotionType.Static, -1.5f, -2.0f, 0.0f));
            //mVirtualSpeakerProperties.Add(new VirtualSpeakerProperty(1, "FrontRight", VirtualSpeakerMotionType.RotateAroundYourHead, 1.5f, -2.0f, 0.0f));

            InitializeComponent();

            dataGridVirtualSpeakerSettings.ItemsSource = mVirtualSpeakerProperties;

            mTextBoxLog.Text = "";
            AddLog(string.Format(CultureInfo.InvariantCulture, "WWSpatialAudioPlayer {0} {1}{2}",
                AssemblyVersion, IntPtr.Size == 8 ? "64bit" : "32bit", Environment.NewLine));

            mBwLoad.DoWork += MBwLoad_DoWork;
            mBwLoad.RunWorkerCompleted += MBwLoad_RunWorkerCompleted;
            mBwLoad.WorkerReportsProgress = true;
            mBwLoad.ProgressChanged += MBwLoad_ProgressChanged;

            UpdateDeviceList();
        }

        private void UpdateDeviceList() {
            mPlayer.SpatialAudio.UpdateDeviceList();
            mListBoxPlaybackDevices.Items.Clear();
            foreach (var item in mPlayer.SpatialAudio.DevicePropertyList) {
                mListBoxPlaybackDevices.Items.Add(string.Format("{0}", item.name));
            }
        }

        #region File Read worker thread stuff
        class LoadParams {
            public string path;
        }

        class LoadProgress {
            public string msg;
            public LoadProgress(string s) {
                msg = s;
            }
        }

        class LoadResult {
            public int hr;
        }

        private void MBwLoad_DoWork(object sender, DoWorkEventArgs e) {
            var param = e.Argument as LoadParams;
            var r = new LoadResult();
            e.Result = r;
            r.hr = 0;

            mBwLoad.ReportProgress(10, new LoadProgress(string.Format("Reading {0}\n", param.path)));

            int hr = mPlayer.ReadAudioFile(param.path);
            if (hr < 0) {
                r.hr = hr;
                return;
            }
            mBwLoad.ReportProgress(66, new LoadProgress("  Resampling...\n"));

            hr = mPlayer.Resample();
            if (hr < 0) {
                r.hr = hr;
                return;
            }
        }
        private void MBwLoad_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            var param = e.UserState as LoadProgress;
            AddLog(param.msg);
            mProgressbar.Value = e.ProgressPercentage;
        }

        private void MBwLoad_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            var r = e.Result as LoadResult;

            if (r.hr < 0) {
                string msg = string.Format(
                    "Error: Read file failed with error code {0:X8} : {1}",
                    r.hr, mTextBoxInputFileName.Text);
                MessageBox.Show(msg);
                AddLog(msg + "\n");

                mButtonPlay.IsEnabled = false;
                mButtonStop.IsEnabled = false;
            } else {
                mButtonPlay.IsEnabled = true;
                AddLog(string.Format("Read succeeded : {0}\n", mTextBoxInputFileName.Text));
                UpdateCheckBoxStaticSpeakers();
            }

            mProgressbar.Value = 0;
        }

        #endregion

        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        private void AddLog(string s) {
            // Console.Write(s);

            // ログを適当なエントリ数で流れるようにする。
            // sは複数行の文字列が入っていたり、改行が入っていなかったりするので、行数制限にはなっていない。
            mLogList.Add(s);
            while (LOG_LINE_NUM < mLogList.Count) {
                mLogList.RemoveAt(0);
            }

            var sb = new StringBuilder();
            foreach (var item in mLogList) {
                sb.Append(item);
            }

            mTextBoxLog.Text = sb.ToString();
            mTextBoxLog.ScrollToEnd();
        }

        private void FilenameTextBoxUpdated() {
            // 特にない。
        }

        private void ReadFile() {
            var param = new LoadParams();
            param.path = mTextBoxInputFileName.Text;

            mBwLoad.RunWorkerAsync(param);
        }

        /// <summary>
        /// AudioObjectTypeMaskの順に並べたチェックボックスのリスト。
        /// </summary>
        private List<CheckBox> StaticSpeakerCheckBoxList() {
            var cbList =new List<CheckBox>();
            cbList.Add(mCbFL);
            cbList.Add(mCbFR);
            cbList.Add(mCbFC);
            cbList.Add(mCbLF);
            cbList.Add(mCbSL);

            cbList.Add(mCbSR);
            cbList.Add(mCbBL);
            cbList.Add(mCbBR);
            cbList.Add(mCbTFL);
            cbList.Add(mCbTFR);

            cbList.Add(mCbTBL);
            cbList.Add(mCbTBR);
            cbList.Add(mCbBFL);
            cbList.Add(mCbBFR);
            cbList.Add(mCbBBL);

            cbList.Add(mCbBBR);
            cbList.Add(mCbBC);
            return cbList;
        }

        private void UpdateCheckBoxStaticSpeakers() {
            var cbList = StaticSpeakerCheckBoxList();

            foreach (var item in cbList) {
                item.IsChecked = false;
            }

            int aoTypeMask = WWSpatialAudioUser.DwChannelMaskToAudioObjectTypeMask(mPlayer.DwChannelMask);
            for (int i=0; i<cbList.Count; ++i) {
                if (0 != (aoTypeMask & (1<<(i + 1)))) {
                    cbList[i].IsChecked = true;
                }
            }
        }

        private int CheckBoxStateToStaticAudioObjectTypeMask() {
            int r = 0;
            var cbList = StaticSpeakerCheckBoxList();
            for (int i = 0; i < cbList.Count; ++i) {
                if (true == cbList[i].IsChecked) {
                    r |= (1 << (i + 1));
                }
            }
            return r;
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        private void dataGridVirtualSpeakerSettings_SelectionChanged(object sender, SelectionChangedEventArgs e) {

        }

        private void ButtonBrowse_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Audio files(*wav;*.flac)|*.wav;*.flac";
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            mTextBoxInputFileName.Text = dlg.FileName;
            FilenameTextBoxUpdated();
            ReadFile();
        }

        private void ButtonPlay_Click(object sender, RoutedEventArgs e) {
            bool b = mPlayer.Play();
            if (b) {
                mButtonPlay.IsEnabled = false;
                mButtonStop.IsEnabled = true;
                mButtonBrowse.IsEnabled = false;
            }
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e) {
            bool b = mPlayer.Stop();
            if (b) {
                mButtonPlay.IsEnabled = true;
                mButtonStop.IsEnabled = false;
                mButtonBrowse.IsEnabled = true;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    if (mBwLoad != null) { 
                        mBwLoad.Dispose();
                        mBwLoad = null;
                    }
                    if (mPlayer != null) {
                        mPlayer.Dispose();
                        mPlayer = null;
                    }
                }

                disposedValue = true;
            }
        }

        void IDisposable.Dispose() {
            // Do not change this code.
            Dispose(true);
        }
        #endregion

        private void Window_Closed(object sender, EventArgs e) {
            Dispose(true);
        }

        private void ButtonUpdatePlaybackDeviceList_Click(object sender, RoutedEventArgs e) {
            UpdateDeviceList();
        }

        private void ButtonRead_Click(object sender, RoutedEventArgs e) {
            int hr = 0;

            string path = mTextBoxInputFileName.Text;

            hr = mPlayer.ReadAudioFile(path);
            if (hr < 0) {
                MessageBox.Show(string.Format("Error: Read file failed. {0}", path));
                return;
            }

            UpdateCheckBoxStaticSpeakers();
        }

        private void ButtonActivateDevice_Click(object sender, RoutedEventArgs e) {
            int devIdx = mListBoxPlaybackDevices.SelectedIndex;
            int maxDynObjCount = 0;
            if (!int.TryParse(mTextBoxMaxDynObjCount.Text, out maxDynObjCount)) {
                MessageBox.Show("Error: MaxDynamicObjectCount is not number");
                return;
            }
            int staticObjMask = CheckBoxStateToStaticAudioObjectTypeMask();

            mPlayer.SpatialAudio.ChooseDevice(devIdx, maxDynObjCount, staticObjMask);
        }

        private void ButtonDeactivateDevice_Click(object sender, RoutedEventArgs e) {

        }
    }
}

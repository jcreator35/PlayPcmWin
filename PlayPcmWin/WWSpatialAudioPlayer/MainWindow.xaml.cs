// 日本語。

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        private bool mInitialized = false;
        private List<VirtualSpeakerProperty> mVirtualSpeakerList = new List<VirtualSpeakerProperty>();
        private SpatialAudioPlayer mPlayer = new SpatialAudioPlayer();
        private BackgroundWorker mBwLoad = new BackgroundWorker();
        /// <summary>
        /// ログの表示行数。
        /// </summary>
        private const int LOG_LINE_NUM = 100;

        private List<string> mLogList = new List<string>();
        private Stopwatch mStopwatch = new Stopwatch();
        private const int MESSAGE_INTERVAL_MS = 1000;
        private StringBuilder mBwMsgSB = new StringBuilder();

        public MainWindow() {
            InitializeComponent();

            mTextBoxLog.Text = "";
            AddLog(string.Format(CultureInfo.InvariantCulture, "WWSpatialAudioPlayer {0} {1}{2}",
                AssemblyVersion, IntPtr.Size == 8 ? "64bit" : "32bit", Environment.NewLine));

            mBwLoad.DoWork += MBwLoad_DoWork;
            mBwLoad.RunWorkerCompleted += MBwLoad_RunWorkerCompleted;
            mBwLoad.WorkerReportsProgress = true;
            mBwLoad.ProgressChanged += MBwLoad_ProgressChanged;

            UpdateDeviceList();
            mStopwatch.Restart();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;
        }

        private void UpdateDeviceList() {
            AddLog("UpdateDeviceList()\n");

            // 以前選択されていたデバイスのIdStr
            var lastSelectedIdStr = "";
            if (0 <= mListBoxPlaybackDevices.SelectedIndex) {
                lastSelectedIdStr = mPlayer.SpatialAudio.DevicePropertyList[
                    mListBoxPlaybackDevices.SelectedIndex].devIdStr;
            }

            mPlayer.SpatialAudio.UpdateDeviceList();
            mListBoxPlaybackDevices.Items.Clear();
            foreach (var item in mPlayer.SpatialAudio.DevicePropertyList) {
                mListBoxPlaybackDevices.Items.Add(string.Format("{0}", item.name));
                if (0 == item.devIdStr.CompareTo(lastSelectedIdStr)) {
                    // 以前選択されていたデバイスを選択状態にする。
                    mListBoxPlaybackDevices.SelectedIndex = mListBoxPlaybackDevices.Items.Count - 1;
                }
            }

            if (0 < mListBoxPlaybackDevices.Items.Count) {
                mButtonActivate.IsEnabled = true;
                mButtonDeactivate.IsEnabled = false;
            } else {
                mButtonActivate.IsEnabled = false;
                mButtonDeactivate.IsEnabled = false;
            }
        }

        #region File Read worker thread stuff
        class LoadParams {
            public string path;
        }

        class LoadResult {
            public int hr;
        }

        private void ReportProgress(int percent, string s) {
            if (MESSAGE_INTERVAL_MS < mStopwatch.ElapsedMilliseconds) {
                // OK
                mBwMsgSB.Append(s);
                mBwLoad.ReportProgress(percent, mBwMsgSB.ToString());
                mBwMsgSB.Clear();
                mStopwatch.Restart();
            } else {
                mBwMsgSB.Append(s);
            }
        }

        private void MBwLoad_DoWork(object sender, DoWorkEventArgs e) {
            mBwMsgSB.Clear();

            var param = e.Argument as LoadParams;
            var r = new LoadResult();
            e.Result = r;
            r.hr = 0;

            ReportProgress(10, string.Format("Reading {0}\n", param.path));

            int hr = mPlayer.ReadAudioFile(param.path);
            if (hr < 0) {
                r.hr = hr;
                return;
            }
            ReportProgress(66, "  Resampling...\n");

            hr = mPlayer.Resample();
            if (hr < 0) {
                r.hr = hr;
                return;
            }

            ReportProgress(90, "  Storing to native buffer...\n");
            hr = mPlayer.StoreSamples();
            if (hr < 0) {
                r.hr = hr;
                return;
            }
        }
        private void MBwLoad_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            var param = e.UserState as string;
            AddLog(param);
            mProgressbar.Value = e.ProgressPercentage;
        }

        private void MBwLoad_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            var r = e.Result as LoadResult;

            if (0 < mPlayer.NumChannels && !mPlayer.IsChannelSupported(mPlayer.NumChannels)) {
                string msg = string.Format(
                    "Error: Audio File of {0}ch is currently not supported!",
                    mPlayer.NumChannels);
                MessageBox.Show(msg);
                AddLog(msg + "\n");
                r.hr = -1;
            }

            if (r.hr < 0) {
                string msg = string.Format(
                    "Error: Read file failed with error code {0:X8} : {1}",
                    r.hr, mTextBoxInputFileName.Text);
                MessageBox.Show(msg);
                AddLog(msg + "\n");

                mGroupBoxPlaybackDevice.IsEnabled = false;
            } else {
                // 成功。
                AddLog(string.Format("Read succeeded : {0}\n", mTextBoxInputFileName.Text));
                mLabelInputAudioFmt.Content = string.Format("File contains {0} ch PCM", mPlayer.NumChannels);
                SelectBestSpeakerConfig();
                UpdateVirtualSpeakerMap();
                mGroupBoxPlaybackDevice.IsEnabled = true;
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

        int NumChannelsToListBoxIdx(int ch) {
            switch (ch) {
            case 2:
                return 0;
            case 6:
                return 1;
            case 8:
                return 2;
            case 12:
            default:
                return 3;
            }
        }

        private int ListBoxSpeakerConfigToNumChannels() {
            int ch = 2;
            if (mListBoxCh6.IsSelected) {
                ch = 6;
            }
            if (mListBoxCh8.IsSelected) {
                ch = 8;
            }
            if (mListBoxCh12.IsSelected) {
                ch = 12;
            }
            return ch;
        }

        private void SelectBestSpeakerConfig() {
            mListBoxSpeakerConfig.SelectedIndex = NumChannelsToListBoxIdx(mPlayer.NumChannels);
        }

        private void UpdateVirtualSpeakerMap() {
            var dwChannelList = WWSpatialAudioUser.DwChannelMaskToList(mPlayer.DwChannelMask);
            System.Diagnostics.Debug.Assert(dwChannelList.Count == mPlayer.NumChannels);

            // NumChとdwChannelList、少ない方のチャンネル数にする。
            int NumCh = ListBoxSpeakerConfigToNumChannels();
            if (dwChannelList.Count < NumCh) {
                NumCh = dwChannelList.Count;
            }

            mStackPanelChannelMap.Children.Clear();
            for (int ch=0; ch< NumCh; ++ch) {
                var lbl = new Label();
                lbl.Content = string.Format("Ch{0} : {1}", ch + 1, dwChannelList[ch]);
                mStackPanelChannelMap.Children.Add(lbl);
            }
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
            bool b = mPlayer.Start();
            if (b) {
                mGroupBoxInputAudioFile.IsEnabled = false;
                mButtonUpdatePlaybackDeviceList.IsEnabled = false;
                mButtonDeactivate.IsEnabled = false;
                mButtonPlay.IsEnabled = false;
                mButtonStop.IsEnabled = true;
            }
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e) {
            bool b = mPlayer.Stop();
            if (b) {
                mGroupBoxInputAudioFile.IsEnabled = true;
                mButtonUpdatePlaybackDeviceList.IsEnabled = false;
                mButtonDeactivate.IsEnabled = true;
                mButtonPlay.IsEnabled = true;
                mButtonStop.IsEnabled = false;
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
            ReadFile();
        }

        const uint E_UNSUPPORTED_TYPE = 0x8007065e;

        private void ButtonActivateDevice_Click(object sender, RoutedEventArgs e) {
            int devIdx = mListBoxPlaybackDevices.SelectedIndex;

            AddLog(string.Format("Activating device #{0} ...\n", devIdx));

            int maxDynObjCount = 0;
            int staticObjMask = WWSpatialAudioUser.DwChannelMaskToAudioObjectTypeMask(mPlayer.DwChannelMask);

            mGroupBoxDeviceList.IsEnabled = false;
            mButtonActivate.IsEnabled = false;
            int hr = mPlayer.SpatialAudio.ChooseDevice(devIdx, maxDynObjCount, staticObjMask);
            if (0 <= hr) {
                // 成功。
                AddLog(string.Format("SpatialAudio.ChooseDevice({0}) success.\n", devIdx));
                mGroupBoxDeviceList.IsEnabled = false;
                mButtonUpdatePlaybackDeviceList.IsEnabled = false;
                mButtonActivate.IsEnabled = false;
                mButtonDeactivate.IsEnabled = true;
                mButtonPlay.IsEnabled = true;
                mButtonStop.IsEnabled = false;
            } else {
                // 失敗。
                if (E_UNSUPPORTED_TYPE == (uint)hr) {
                    var s = string.Format("Error: Spatial Audio of the specified device is not enabled! Please enable Spatial Audio of the device.\n", devIdx);
                    AddLog(s);
                    MessageBox.Show(s);
                } else {
                    var s = string.Format("SpatialAudio.ChooseDevice({0}) failed with error {1:X8}.\n", devIdx, hr);
                    AddLog(s);
                    MessageBox.Show(s);
                }
                mGroupBoxDeviceList.IsEnabled = true;
                mButtonUpdatePlaybackDeviceList.IsEnabled = true;
                mButtonActivate.IsEnabled = true;
                mButtonDeactivate.IsEnabled = false;
                mButtonPlay.IsEnabled = false;
                mButtonStop.IsEnabled = false;
            }
        }

        private void ButtonDeactivateDevice_Click(object sender, RoutedEventArgs e) {
            int hr = mPlayer.SpatialAudio.ChooseDevice(-1, 0, 0);
            AddLog(string.Format("SpatialAudio.ChooseDevice(-1) hr={0:X8}\n", hr));

            mGroupBoxDeviceList.IsEnabled = true;
            mButtonUpdatePlaybackDeviceList.IsEnabled = true;
            mButtonActivate.IsEnabled = true;
            mButtonDeactivate.IsEnabled = false;
            mButtonPlay.IsEnabled = false;
            mButtonStop.IsEnabled = false;

            UpdateDeviceList();
        }

        private void ListBoxSpeakerConfig_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            UpdateVirtualSpeakerMap();
        }

    }
}

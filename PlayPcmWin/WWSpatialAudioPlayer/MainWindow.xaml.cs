// 日本語。

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using WWSpatialAudioUserCs;

namespace WWSpatialAudioPlayer {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IDisposable {
        private bool mInitialized = false;
        private bool mShuttingdown = false;
        private List<VirtualSpeakerProperty> mVirtualSpeakerList = new List<VirtualSpeakerProperty>();
        private SpatialAudioPlayer mPlayer = new SpatialAudioPlayer();
        private BackgroundWorker mBwLoad = new BackgroundWorker();
        private BackgroundWorker mBwPlay = new BackgroundWorker();

        /// <summary>
        /// ログの表示行数。
        /// </summary>
        private const int LOG_LINE_NUM = 100;

        private const uint E_UNSUPPORTED_TYPE = 0x8007065e;
        private const int E_ABORT = -128;


        private string NO_DURATION_STR = "--:-- / --:--";

        private List<string> mLogList = new List<string>();
        private Stopwatch mSWProgressReport = new Stopwatch();
        private const int MESSAGE_INTERVAL_MS = 1000;
        private StringBuilder mBwMsgSB = new StringBuilder();
        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        public MainWindow() {
            InitializeComponent();

            mTextBoxLog.Text = "";
            AddLog(string.Format(CultureInfo.InvariantCulture, "WWSpatialAudioPlayer {0} {1}{2}",
                AssemblyVersion, IntPtr.Size == 8 ? "64bit" : "32bit", Environment.NewLine));

            mBwLoad.DoWork += MBwLoad_DoWork;
            mBwLoad.RunWorkerCompleted += MBwLoad_RunWorkerCompleted;
            mBwLoad.WorkerReportsProgress = true;
            mBwLoad.ProgressChanged += MBwLoad_ProgressChanged;
            mBwLoad.WorkerSupportsCancellation = true;

            mBwPlay.DoWork += MBwPlay_DoWork;
            mBwPlay.RunWorkerCompleted += MBwPlay_RunWorkerCompleted;
            mBwPlay.WorkerReportsProgress = true;
            mBwPlay.ProgressChanged += MBwPlay_ProgressChanged;
            mBwPlay.WorkerSupportsCancellation = true;

            mTextBoxInputFileName.Text = Properties.Settings.Default.LastReadFileName;

            UpdateDeviceList();
            mSWProgressReport.Restart();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            SetupSlider();

            mInitialized = true;

            mBwPlay.RunWorkerAsync();
        }

        /// <summary>
        ///  プログラムを即時終了する。
        /// </summary>
        private void Exit() {
            Close();
        }

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    mShuttingdown = true;

                    mBwPlay.CancelAsync();
                    while (mBwPlay.IsBusy) {
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                                System.Windows.Threading.DispatcherPriority.Background,
                                new System.Threading.ThreadStart(delegate { }));
                        System.Threading.Thread.Sleep(100);
                    }

                    mBwLoad.CancelAsync();
                    while (mBwLoad.IsBusy) {
                        System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                                System.Windows.Threading.DispatcherPriority.Background,
                                new System.Threading.ThreadStart(delegate { }));
                        System.Threading.Thread.Sleep(100);
                    }

                    if (mBwPlay != null) {
                        mBwPlay.Dispose();
                        mBwPlay = null;
                    }
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

        private void Window_Closing(object sender, CancelEventArgs e) {
            Properties.Settings.Default.LastReadFileName = mTextBoxInputFileName.Text;
            Properties.Settings.Default.Save();
        }

        private void Window_Closed(object sender, EventArgs e) {
            Dispose(true);
        }

        #endregion

        #region state stuff
        enum State {
            NoSoundDevices,
            Deactivated,
            Activated,
            Playing,
        }

        private State mState = State.NoSoundDevices;

        private void UpdateUIState(State s) {
            switch (s) {
            case State.NoSoundDevices:
                mGroupBoxInputAudioFile.IsEnabled = true;
                mGroupBoxDeviceList.IsEnabled = true;
                mButtonUpdatePlaybackDeviceList.IsEnabled = true;
                mButtonActivate.IsEnabled = false;
                mButtonDeactivate.IsEnabled = false;
                mButtonPlay.IsEnabled = false;
                mButtonStop.IsEnabled = false;
                break;
            case State.Deactivated:
                mGroupBoxInputAudioFile.IsEnabled = true;
                mGroupBoxDeviceList.IsEnabled = true;
                mButtonUpdatePlaybackDeviceList.IsEnabled = true;
                mButtonActivate.IsEnabled = true;
                mButtonDeactivate.IsEnabled = false;
                mButtonPlay.IsEnabled = false;
                mButtonStop.IsEnabled = false;
                break;
            case State.Activated:
                mGroupBoxInputAudioFile.IsEnabled = true;
                mGroupBoxDeviceList.IsEnabled = false;
                mButtonUpdatePlaybackDeviceList.IsEnabled = false;
                mButtonActivate.IsEnabled = false;
                mButtonDeactivate.IsEnabled = true;
                mButtonPlay.IsEnabled = true;
                mButtonStop.IsEnabled = false;
                mLabelPlayingTime.Content = NO_DURATION_STR;
                mSliderPlayPosion.Value = 0;
                break;
            case State.Playing:
                mGroupBoxInputAudioFile.IsEnabled = false;
                mGroupBoxDeviceList.IsEnabled = false;
                mButtonUpdatePlaybackDeviceList.IsEnabled = false;
                mButtonDeactivate.IsEnabled = false;
                mButtonPlay.IsEnabled = false;
                mButtonStop.IsEnabled = true;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
            mState = s;
        }

        #endregion

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

        private void UpdateDeviceList() {
            AddLog("UpdateDeviceList()\n");

            // 以前選択されていたデバイスのIdStr
            var lastSelectedIdStr = Properties.Settings.Default.LastUsedDevice;
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
                UpdateUIState(State.Deactivated);
            } else {
                UpdateUIState(State.NoSoundDevices);
            }
        }

        #region File Read worker thread stuff
        class LoadParams {
            public string path;
        }

        class LoadResult {
            public int hr;
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
            if (mBwLoad.CancellationPending) {
                e.Cancel = true;
                r.hr = E_ABORT;
                return;
            }

            ReportProgress(66, "  Resampling...\n");

            hr = mPlayer.Resample();
            if (hr < 0) {
                r.hr = hr;
                return;
            }
            if (mBwLoad.CancellationPending) {
                e.Cancel = true;
                r.hr = E_ABORT;
                return;
            }

            ReportProgress(90, "  Storing to native buffer...\n");
            hr = mPlayer.StoreSamplesToNativeBuffer();
            if (hr < 0) {
                r.hr = hr;
                return;
            }
        }

        private void ReportProgress(int percent, string s) {
            if (MESSAGE_INTERVAL_MS < mSWProgressReport.ElapsedMilliseconds) {
                // OK
                mBwMsgSB.Append(s);
                mBwLoad.ReportProgress(percent, mBwMsgSB.ToString());
                mBwMsgSB.Clear();
                mSWProgressReport.Restart();
            } else {
                mBwMsgSB.Append(s);
            }
        }

        private void MBwLoad_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            if (mBwLoad.CancellationPending || mShuttingdown) {
                return;
            }

            var param = e.UserState as string;
            AddLog(param);
            mProgressbar.Value = e.ProgressPercentage;
        }

        private void MBwLoad_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (e.Cancelled) {
                return;
            }

            mGroupBoxInputAudioFile.IsEnabled = true;

            if (0 < mBwMsgSB.Length) {
                AddLog(mBwMsgSB.ToString());
                mBwMsgSB.Clear();
            }

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
                mLabelInputAudioFmt.Content = string.Format("File contains {0} ch PCM, channel to speaker map = {1}",
                    mPlayer.NumChannels, DwChannelMaskToStr(mPlayer.DwChannelMask));

                mGroupBoxPlaybackDevice.IsEnabled = true;
            }

            mProgressbar.Value = 0;
        }

        private string DwChannelMaskToStr(int dwChannelMask) {
            var sb = new StringBuilder();

            foreach (var item in WWSpatialAudioUser.DwChannelMaskToList(dwChannelMask)) {
                sb.AppendFormat("{0} ", WWSpatialAudioUser.DwChannelMaskShortStr(item));
            }

            return sb.ToString().TrimEnd(new char[] {' '});
        }

        private void ReadFile() {
            var param = new LoadParams();
            param.path = mTextBoxInputFileName.Text;
            mGroupBoxInputAudioFile.IsEnabled = false;

            mBwLoad.RunWorkerAsync(param);
        }

        #endregion

        #region Play time disp update worker thread

        private const int PLAY_TIME_UPDATE_THREAD_WAKEUP_INTERVAL_MS = 100;

        private void MBwPlay_DoWork(object sender, DoWorkEventArgs e) {
            while (!mBwPlay.CancellationPending) {
                System.Threading.Thread.Sleep(PLAY_TIME_UPDATE_THREAD_WAKEUP_INTERVAL_MS);
                mBwPlay.ReportProgress(0);

                int hr = mPlayer.SpatialAudio.GetThreadErcd();
                if (hr < 0) {
                    break;
                }
            }

            if (mBwPlay.CancellationPending) {
                e.Cancel = true;
            }
        }

        private static string SecondsToMSString(int seconds) {
            int m = seconds / 60;
            int s = seconds - m * 60;
            return string.Format(CultureInfo.CurrentCulture, "{0:D2}:{1:D2}", m, s);
        }

        private void MBwPlay_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            if (mBwPlay.CancellationPending || mShuttingdown) {
                return;
            }

            var pp = mPlayer.SpatialAudio.GetPlayStatus(0);

            string s = NO_DURATION_STR;
            if (0 <= pp.TrackNr) {
                string sD = SecondsToMSString((int)(pp.TotalFrameNum / mPlayer.PlaySampleRate));
                string sP = SecondsToMSString((int)(pp.PosFrame / mPlayer.PlaySampleRate));
                s = string.Format("{0} / {1}", sP, sD);
            }

            UpdateSliderPosition(pp);

            if (mState == State.Playing && pp.TrackNr == (int)WWSpatialAudioUser.TrackTypeEnum.None) {
                // 再生→再生停止。
                UpdateUIState(State.Activated);
            }

            if (0 < s.CompareTo(mLabelPlayingTime.Content.ToString())) {
                // 時間が変わったので描画更新。
                mLabelPlayingTime.Content = s;
            }
        }

        private void MBwPlay_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            if (mShuttingdown) {
                return;
            }

            int hr = mPlayer.SpatialAudio.GetThreadErcd();
            if (hr < 0) {  
                MessageBox.Show(string.Format("Unrecoverable error: Playback thread encountered error {0:X8} !\nProgram will exit.", hr));
                Exit();
                return;
            }
        }

        #endregion

        #region Slider event

        private long mLastSliderValue = 0;
        private bool mSliderSliding = false;
        private long mLastSliderPositionUpdateTime = 0;

        /// <summary>
        /// スライダー位置の更新頻度 (500ミリ秒)
        /// </summary>
        private const long SLIDER_UPDATE_TICKS = 500 * 10000;

        private void SetupSlider() {
            // sliderのTrackをクリックしてThumbがクリック位置に移動した時Thumbがつままれた状態になるようにする
            mSliderPlayPosion.ApplyTemplate();
            (mSliderPlayPosion.Template.FindName("PART_Track", mSliderPlayPosion) as Track).Thumb.MouseEnter += new MouseEventHandler((sliderSender, se) => {
                if (se.LeftButton == MouseButtonState.Pressed && se.MouseDevice.Captured == null) {
                    var args = new MouseButtonEventArgs(se.MouseDevice, se.Timestamp, MouseButton.Left);
                    args.RoutedEvent = MouseLeftButtonDownEvent;
                    (sliderSender as Thumb).RaiseEvent(args);
                }
            });
        }

        private void UpdateSliderPosition(WWSpatialAudioUser.PlayStatus ps) {

            long now = DateTime.Now.Ticks;
            if (now - mLastSliderPositionUpdateTime > SLIDER_UPDATE_TICKS) {
                // スライダー位置の更新。0.5秒に1回

                //Console.WriteLine("SliderPos={0} / {1}", playPos.PosFrame, playPos.TotalFrameNum);

                mSliderPlayPosion.Maximum = ps.TotalFrameNum;

                if (!mSliderSliding || ps.TotalFrameNum <= mSliderPlayPosion.Value) {
                    if (ps.TrackNr < 0) {
                        // prologue, epilogue
                        mSliderPlayPosion.Value = 0;
                    } else {
                        mSliderPlayPosion.Value = ps.PosFrame;
                    }
                }

                mLastSliderPositionUpdateTime = now;
            }
        }

        private void MSliderPlayPosion_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (e.Source != mSliderPlayPosion) {
                return;
            }

            mLastSliderValue = (long)mSliderPlayPosion.Value;
            mSliderSliding = true;
        }

        private void MSliderPlayPosion_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
            if (e.Source != mSliderPlayPosion) {
                return;
            }

            if (e.LeftButton == MouseButtonState.Pressed) {
                if (!mButtonPlay.IsEnabled &&
                        mLastSliderValue != (long)mSliderPlayPosion.Value) {
                    // 再生中。再生位置を変更する。
                    mPlayer.SpatialAudio.SetPlayPos((long)mSliderPlayPosion.Value);
                    mLastSliderValue = (long)mSliderPlayPosion.Value;
                }
            }
        }

        private void MSliderPlayPosion_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (e.Source != mSliderPlayPosion) {
                return;
            }

            if (!mButtonPlay.IsEnabled &&
                    mLastSliderValue != (long)mSliderPlayPosion.Value) {
                // 再生中。再生位置を変更する。
                mPlayer.SpatialAudio.SetPlayPos((long)mSliderPlayPosion.Value);
            }

            mLastSliderValue = 0;
            mSliderSliding = false;
        }

        #endregion

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // Event handling

        private void ButtonBrowse_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Audio files(*wav;*.flac)|*.wav;*.flac";
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            mTextBoxInputFileName.Text = dlg.FileName;
            ReadFile();
        }

        private void ButtonUpdatePlaybackDeviceList_Click(object sender, RoutedEventArgs e) {
            UpdateDeviceList();
        }

        private void ButtonRead_Click(object sender, RoutedEventArgs e) {
            ReadFile();
        }

        private void ButtonActivateDevice_Click(object sender, RoutedEventArgs e) {
            int hr = 0;
            int devIdx = mListBoxPlaybackDevices.SelectedIndex;

            Properties.Settings.Default.LastUsedDevice = mPlayer.SpatialAudio.DevicePropertyList[
                    mListBoxPlaybackDevices.SelectedIndex].devIdStr;


            {
                int maxDynObjCount = 0;
                int staticObjMask = WWSpatialAudioUser.DwChannelMaskToAudioObjectTypeMask(mPlayer.DwChannelMask);

                hr = mPlayer.SpatialAudio.ChooseDevice(devIdx, maxDynObjCount, staticObjMask);
            }
            if (0 <= hr) {
                // Activate成功。
                AddLog(string.Format("SpatialAudio.ChooseDevice({0}) success.\n", devIdx));

                // 無音送出開始。
                mPlayer.SpatialAudio.SetCurrentPcm(
                    WWSpatialAudioUser.TrackTypeEnum.None,
                    WWSpatialAudioUser.ChangeTrackMethod.Immediately);
                hr = mPlayer.Start();
                if (hr < 0) {
                    var s = string.Format("SpatialAudio.Start({0}) failed with error {1:X8}.\n", devIdx, hr);
                    AddLog(s);
                    MessageBox.Show(s);
                    hr = mPlayer.SpatialAudio.ChooseDevice(-1, 0, 0);
                    AddLog(string.Format("SpatialAudio.ChooseDevice(-1)\n"));
                    return;
                }

                // 全て成功。
                UpdateUIState(State.Activated);
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
                UpdateUIState(State.Deactivated);
            }
        }

        private void ButtonDeactivateDevice_Click(object sender, RoutedEventArgs e) {
            mPlayer.Stop();

            int hr = mPlayer.SpatialAudio.ChooseDevice(-1, 0, 0);
            AddLog(string.Format("SpatialAudio.ChooseDevice(-1) hr={0:X8}\n", hr));

            mPlayer.SpatialAudio.Rewind();

            UpdateDeviceList();
        }

        private void ButtonPlay_Click(object sender, RoutedEventArgs e) {
            mPlayer.SpatialAudio.Rewind();
            mPlayer.SpatialAudio.SetCurrentPcm(
                WWSpatialAudioUser.TrackTypeEnum.Prologue,
                WWSpatialAudioUser.ChangeTrackMethod.Immediately);

            AddLog(string.Format("SetCurrentPcm(Prologue)\n"));

            UpdateUIState(State.Playing);
        }

        private void ButtonStop_Click(object sender, RoutedEventArgs e) {
            mPlayer.SpatialAudio.SetCurrentPcm(
                WWSpatialAudioUser.TrackTypeEnum.Epilogue,
                WWSpatialAudioUser.ChangeTrackMethod.Crossfade);
            AddLog(string.Format("SetCurrentPcm(Epilogue)\n"));

            // BwPlayスレッドで、再生物が無くなったことを検知してActivatedに遷移。
        }

    }
}

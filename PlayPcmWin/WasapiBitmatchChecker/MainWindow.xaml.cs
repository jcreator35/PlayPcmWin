using System;
using System.Windows;
using Wasapi;
using System.ComponentModel;
using System.Windows.Threading;
using System.Text;
using System.Threading;

namespace WasapiBitmatchChecker {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        enum State {
            Init,
            Syncing,
            Running,
            RecCompleted,
        };

        private WasapiCS mWasapiPlay;
        private WasapiCS mWasapiRec;

        private BackgroundWorker mPlayWorker;
        private BackgroundWorker mRecWorker;

        private Wasapi.WasapiCS.CaptureCallback mCaptureDataArrivedDelegate;

        private static int NUM_PROLOGUE_FRAMES = 262144;
        private int mNumTestFrames = 1024 * 1024;
        private static int NUM_CHANNELS = 2;
        private int mSampleRate;
        private WasapiCS.SampleFormatType mPlaySampleFormat;
        private WasapiCS.SampleFormatType mRecSampleFormat;
        private WasapiCS.DataFeedMode mPlayDataFeedMode;
        private WasapiCS.DataFeedMode mRecDataFeedMode;
        private int mPlayBufferMillisec;
        private int mRecBufferMillisec;

        private DispatcherTimer mSyncTimeout;

        private State mState = State.Init;

        private PcmDataLib.PcmData mPcmSync;
        private PcmDataLib.PcmData mPcmReady;
        private PcmDataLib.PcmData mPcmTest;
        private PcmDataLib.PcmData mPcmRecorded;

        Random mRand = new Random();

        private byte[] mCapturedPcmData;
        private int mCapturedBytes;

        private Object mLock = new Object();

        private Wasapi.WasapiCS.StateChangedCallback mStateChanged;

        private void LocalizeUI() {
            groupBoxPcmDataSettings.Header = Properties.Resources.groupBoxPcmDataSettings;
            groupBoxSampleRate.Header = Properties.Resources.groupBoxSampleRate;
            groupBoxDataPattern.Header = Properties.Resources.groupBoxDataPattern;
            groupBoxPlayback.Header = Properties.Resources.groupBoxPlayback;
            groupBoxPlaybackDevice.Header = Properties.Resources.groupBoxPlaybackDevice;
            groupBoxPlaybackDataFeedMode.Header = Properties.Resources.groupBoxDataFeed;
            groupBoxPlayBufferSize.Header = Properties.Resources.groupBoxBufferSize;
            groupBoxPlayPcmFormat.Header = Properties.Resources.groupBoxPcmFormat;
            groupBoxRecording.Header = Properties.Resources.groupBoxRecording;
            groupBoxRecordingDevice.Header = Properties.Resources.groupBoxRecordingDevice;
            groupBoxRecordingDataFeed.Header = Properties.Resources.groupBoxDataFeed;
            groupBoxRecordingBufferSize.Header = Properties.Resources.groupBoxBufferSize;
            groupBoxRecPcmFormat.Header = Properties.Resources.groupBoxPcmFormat;
            groupBoxLog.Header = Properties.Resources.groupBoxLog;
            radioButtonPcmRandom.Content = Properties.Resources.radioButtonPcmRandom;
            labelPcmSize.Content = Properties.Resources.labelPcmSize;
            radioButtonPlayEvent.Content = Properties.Resources.radioButtonEventDriven;
            radioButtonPlayTimer.Content = Properties.Resources.radioButtonTimerDriven;
            radioButtonPlayPcm16.Content = Properties.Resources.radioButtonSint16;
            radioButtonPlayPcm24.Content = Properties.Resources.radioButtonSint24;
            radioButtonPlayPcm32v24.Content = Properties.Resources.radioButtonSint32v24;
            radioButtonRecEvent.Content = Properties.Resources.radioButtonEventDriven;
            radioButtonRecTimer.Content = Properties.Resources.radioButtonTimerDriven;
            radioButtonRecPcm16.Content = Properties.Resources.radioButtonSint16;
            radioButtonRecPcm24.Content = Properties.Resources.radioButtonSint24;
            radioButtonRecPcm32v24.Content = Properties.Resources.radioButtonSint32v24;
            buttonStart.Content = Properties.Resources.buttonStart;
            buttonStop.Content = Properties.Resources.buttonStop;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            LocalizeUI();

            mWasapiPlay = new WasapiCS();
            mWasapiPlay.Init();

            mWasapiRec = new WasapiCS();
            mWasapiRec.Init();
            mCaptureDataArrivedDelegate = new Wasapi.WasapiCS.CaptureCallback(CaptureDataArrived);
            mWasapiRec.RegisterCaptureCallback(mCaptureDataArrivedDelegate);

            mPlayWorker = new BackgroundWorker();
            mPlayWorker.DoWork += new DoWorkEventHandler(PlayDoWork);
            mPlayWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PlayRunWorkerCompleted);
            mPlayWorker.WorkerSupportsCancellation = true;
            mPlayWorker.WorkerReportsProgress = true;
            mPlayWorker.ProgressChanged += new ProgressChangedEventHandler(PlayWorkerProgressChanged);

            mRecWorker = new BackgroundWorker();
            mRecWorker.DoWork += new DoWorkEventHandler(RecDoWork);
            mRecWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(RecRunWorkerCompleted);
            mRecWorker.WorkerSupportsCancellation = true;

            UpdateDeviceList();

            mSyncTimeout = new DispatcherTimer();
            mSyncTimeout.Tick += new EventHandler(SyncTimeoutTickCallback);
            mSyncTimeout.Interval = new TimeSpan(0, 0, 5);

            textBoxLog.Text = string.Format("WasapiBitmatchChecker version {0}\r\n", AssemblyVersion);

            mStateChanged = new Wasapi.WasapiCS.StateChangedCallback(StateChangedCallback);
            mWasapiPlay.RegisterStateChangedCallback(mStateChanged);
        }

        public void StateChangedCallback(StringBuilder idStr) {
            Dispatcher.BeginInvoke(new Action(delegate() {
                lock (mLock) {
                    if (mState == State.Init) {
                        StopUnsetup();
                        UpdateDeviceList(); //< この中でbuttonStart.IsEnabledの状態が適切に更新される
                    } else {
                        var playDevice = listBoxPlayDevices.SelectedItem as string;
                        if (playDevice.Equals(idStr.ToString())) {
                            Term();
                            MessageBox.Show(string.Format(Properties.Resources.msgPlayDeviceStateChanged, playDevice));
                            Close();
                        }

                        var recDevice = listBoxRecDevices.SelectedItem as string;
                        if (recDevice.Equals(idStr.ToString())) {
                            Term();
                            MessageBox.Show(string.Format(Properties.Resources.msgRecDeviceStateChanged, recDevice));
                            Close();
                        }
                    }
                }
            }));
        }

        private void UpdateDeviceList() {
            {
                mWasapiPlay.EnumerateDevices(WasapiCS.DeviceType.Play);
                string prevDevice = string.Empty;
                if (0 <= listBoxPlayDevices.SelectedIndex) {
                    prevDevice = listBoxPlayDevices.SelectedItem as string;
                }

                listBoxPlayDevices.Items.Clear();
                for (int i=0; i < mWasapiPlay.GetDeviceCount(); ++i) {
                    var attr = mWasapiPlay.GetDeviceAttributes(i);
                    listBoxPlayDevices.Items.Add(attr.Name);
                    if (attr.Name.Equals(prevDevice)) {
                        listBoxPlayDevices.SelectedIndex = i;
                    }
                }
            }

            {
                mWasapiRec.EnumerateDevices(WasapiCS.DeviceType.Rec);
                string prevDevice = string.Empty;
                if (0 <= listBoxRecDevices.SelectedIndex) {
                    prevDevice = listBoxRecDevices.SelectedItem as string;
                }

                listBoxRecDevices.Items.Clear();
                for (int i=0; i < mWasapiRec.GetDeviceCount(); ++i) {
                    var attr = mWasapiRec.GetDeviceAttributes(i);
                    listBoxRecDevices.Items.Add(attr.Name);
                    if (attr.Name.Equals(prevDevice)) {
                        listBoxRecDevices.SelectedIndex = i;
                    }
                }
            }

            if (0 < listBoxPlayDevices.Items.Count &&
                    0 < listBoxRecDevices.Items.Count) {
                buttonStart.IsEnabled = true;
            } else {
                buttonStart.IsEnabled = false;
            }
        }

        private void Exit() {
            Term();
            Close();
        }

        private void Term() {
            // バックグラウンドスレッドにjoinして、完全に止まるまで待ち合わせするブロッキング版のStopを呼ぶ。
            // そうしないと、バックグラウンドスレッドによって使用中のオブジェクトが
            // この後のUnsetupの呼出によって開放されてしまい問題が起きる。
            StopBlocking();

            if (mWasapiRec != null) {
                mWasapiRec.Unsetup();
                mWasapiRec.Term();
                mWasapiRec = null;
            }

            if (mWasapiPlay != null) {
                mWasapiPlay.Unsetup();
                mWasapiPlay.Term();
                mWasapiPlay = null;
            }
        }

        private void Window_Closed(object sender, EventArgs e) {
            Term();
        }

        private void StopBlocking() {
            if (mRecWorker.IsBusy) {
                mRecWorker.CancelAsync();
            }
            while (mRecWorker.IsBusy) {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new System.Threading.ThreadStart(delegate { }));

                System.Threading.Thread.Sleep(100);
            }

            if (mPlayWorker.IsBusy) {
                mPlayWorker.CancelAsync();
            }
            while (mPlayWorker.IsBusy) {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                        System.Windows.Threading.DispatcherPriority.Background,
                        new System.Threading.ThreadStart(delegate { }));

                System.Threading.Thread.Sleep(100);
            }
        }

        private bool UpdateTestParamsFromUI() {

            if (!Int32.TryParse(textBoxTestFrames.Text, out mNumTestFrames) || mNumTestFrames <= 0) {
                MessageBox.Show(Properties.Resources.msgPcmSizeError);
                return false;
            }
            if (0x7fffffff / 8 / 1024 / 1024 < mNumTestFrames) {
                MessageBox.Show(string.Format(Properties.Resources.msgPcmSizeTooLarge, 0x70000000 / 8 / 1024 / 1024));
                return false;
            }
            mNumTestFrames *= 1024 * 1024;

            if (radioButton44100.IsChecked == true) {
                mSampleRate = 44100;
            }
            if (radioButton48000.IsChecked == true) {
                mSampleRate = 48000;
            }
            if (radioButton88200.IsChecked == true) {
                mSampleRate = 88200;
            }
            if (radioButton96000.IsChecked == true) {
                mSampleRate = 96000;
            }
            if (radioButton176400.IsChecked == true) {
                mSampleRate = 176400;
            }
            if (radioButton192000.IsChecked == true) {
                mSampleRate = 192000;
            }

            if (radioButtonPlayPcm16.IsChecked == true) {
                mPlaySampleFormat = WasapiCS.SampleFormatType.Sint16;
            }
            if (radioButtonPlayPcm24.IsChecked == true) {
                mPlaySampleFormat = WasapiCS.SampleFormatType.Sint24;
            }
            if (radioButtonPlayPcm32v24.IsChecked == true) {
                mPlaySampleFormat = WasapiCS.SampleFormatType.Sint32V24;
            }

            if (radioButtonRecPcm16.IsChecked == true) {
                mRecSampleFormat = WasapiCS.SampleFormatType.Sint16;
            }
            if (radioButtonRecPcm24.IsChecked == true) {
                mRecSampleFormat = WasapiCS.SampleFormatType.Sint24;
            }
            if (radioButtonRecPcm32v24.IsChecked == true) {
                mRecSampleFormat = WasapiCS.SampleFormatType.Sint32V24;
            }

            if (radioButtonPlayEvent.IsChecked == true) {
                mPlayDataFeedMode = WasapiCS.DataFeedMode.EventDriven;
            }
            if (radioButtonPlayTimer.IsChecked == true) {
                mPlayDataFeedMode = WasapiCS.DataFeedMode.TimerDriven;
            }

            if (radioButtonRecEvent.IsChecked == true) {
                mRecDataFeedMode = WasapiCS.DataFeedMode.EventDriven;
            }
            if (radioButtonRecTimer.IsChecked == true) {
                mRecDataFeedMode = WasapiCS.DataFeedMode.TimerDriven;
            }

            if (!Int32.TryParse(textBoxPlayBufferSize.Text, out mPlayBufferMillisec)) {
                MessageBox.Show(Properties.Resources.msgPlayBufferSizeError);
                return false;
            }
            if (mPlayBufferMillisec <= 0 || 1000 <= mPlayBufferMillisec) {
                MessageBox.Show(Properties.Resources.msgPlayBufferSizeTooLarge);

            }
            if (!Int32.TryParse(textBoxRecBufferSize.Text, out mRecBufferMillisec)) {
                MessageBox.Show(Properties.Resources.msgRecBufferSizeError);
                return false;
            }
            if (mRecBufferMillisec <= 0 || 1000 <= mRecBufferMillisec) {
                MessageBox.Show(Properties.Resources.msgRecBufferSizeTooLarge);

            }
            return true;
        }

        private void PreparePcmData() {
            mPcmSync  = new PcmDataLib.PcmData();
            mPcmReady = new PcmDataLib.PcmData();
            mPcmTest  = new PcmDataLib.PcmData();

            mPcmSync.SetFormat(NUM_CHANNELS,
                    WasapiCS.SampleFormatTypeToUseBitsPerSample(mPlaySampleFormat),
                    WasapiCS.SampleFormatTypeToValidBitsPerSample(mPlaySampleFormat),
                    mSampleRate,
                    PcmDataLib.PcmData.ValueRepresentationType.SInt, mSampleRate);
            var data = new byte[(WasapiCS.SampleFormatTypeToUseBitsPerSample(mPlaySampleFormat) / 8) * NUM_CHANNELS * mPcmSync.NumFrames];
            mPcmSync.SetSampleArray(data);

            mPcmReady.CopyFrom(mPcmSync);
            data = new byte[(WasapiCS.SampleFormatTypeToUseBitsPerSample(mPlaySampleFormat) / 8) * NUM_CHANNELS * mPcmSync.NumFrames];
            mPcmReady.SetSampleArray(data);

            mPcmTest.CopyFrom(mPcmSync);
            data = new byte[(WasapiCS.SampleFormatTypeToUseBitsPerSample(mPlaySampleFormat) / 8) * NUM_CHANNELS * mNumTestFrames];
            mRand.NextBytes(data);
            mPcmTest.SetSampleArray(mNumTestFrames, data);

            mCapturedPcmData = new byte[(WasapiCS.SampleFormatTypeToUseBitsPerSample(mRecSampleFormat) / 8) * NUM_CHANNELS * (mNumTestFrames + NUM_PROLOGUE_FRAMES)];
            Array.Clear(mCapturedPcmData, 0, mCapturedPcmData.Length);

            switch (mPlaySampleFormat) {
            case WasapiCS.SampleFormatType.Sint16:
                mPcmSync.SetSampleValueInInt32(0, 0, 0x00040000);
                mPcmReady.SetSampleValueInInt32(0, 0, 0x00030000);
                break;
            case WasapiCS.SampleFormatType.Sint24:
            case WasapiCS.SampleFormatType.Sint32V24:
                mPcmSync.SetSampleValueInInt32(0, 0, 0x00000400);
                mPcmReady.SetSampleValueInInt32(0, 0, 0x00000300);
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
        }

        /// <summary>
        /// 再生中。バックグラウンドスレッド。
        /// </summary>
        private void PlayDoWork(object o, DoWorkEventArgs args) {
            //Console.WriteLine("PlayDoWork started");
            BackgroundWorker bw = (BackgroundWorker)o;

            while (!mWasapiPlay.Run(100)) {
                System.Threading.Thread.Sleep(1);
                if (bw.CancellationPending) {
                    Console.WriteLine("PlayDoWork() CANCELED");
                    mWasapiPlay.Stop();
                    args.Cancel = true;
                }
                
                var playPosition = mWasapiPlay.GetPlayCursorPosition(WasapiCS.PcmDataUsageType.NowPlaying);
                if (playPosition.TotalFrameNum == mNumTestFrames) {
                    // 本編を再生している時だけプログレスバーを動かす
                    mPlayWorker.ReportProgress((int)(playPosition.PosFrame * 95 / playPosition.TotalFrameNum));
                }
            }

            // 正常に最後まで再生が終わった場合、ここでStopを呼んで、後始末する。
            // キャンセルの場合は、2回Stopが呼ばれることになるが、問題ない!!!
            mWasapiPlay.Stop();

            // 停止完了後タスクの処理は、ここではなく、PlayRunWorkerCompletedで行う。
        }

        private void PlayWorkerProgressChanged(object sender, ProgressChangedEventArgs e) {
            progressBar1.Value = e.ProgressPercentage;
        }

        /// <summary>
        /// 再生終了。
        /// </summary>
        private void PlayRunWorkerCompleted(object o, RunWorkerCompletedEventArgs args) {
            mWasapiPlay.Unsetup();
            // このあと録音も程なく終わり、RecRunWorkerCompletedでデバイス一覧表示は更新される。
        }

        private void RecDoWork(object o, DoWorkEventArgs args) {
            BackgroundWorker bw = (BackgroundWorker)o;

            while (!mWasapiRec.Run(100) && mState != State.RecCompleted) {
                System.Threading.Thread.Sleep(1);
                if (bw.CancellationPending) {
                    Console.WriteLine("RecDoWork() CANCELED");
                    mWasapiRec.Stop();
                    args.Cancel = true;
                }
            }

            // キャンセルの場合は、2回Stopが呼ばれることになるが、問題ない!!!
            mWasapiRec.Stop();

            // 停止完了後タスクの処理は、ここではなく、RecRunWorkerCompletedで行う。
        }

        private void RecRunWorkerCompleted(object o, RunWorkerCompletedEventArgs args) {
            lock (mLock) {
                mWasapiRec.Unsetup();

                CompareRecordedData();

                // 完了。UIの状態を戻す。
                if (0 < listBoxPlayDevices.Items.Count &&
                        0 < listBoxRecDevices.Items.Count) {
                    buttonStart.IsEnabled = true;
                } else {
                    buttonStart.IsEnabled = false;
                }
                buttonStop.IsEnabled = false;

                groupBoxPcmDataSettings.IsEnabled = true;
                groupBoxPlayback.IsEnabled = true;
                groupBoxRecording.IsEnabled = true;

                progressBar1.Value = 0;

                mState = State.Init;
            }
        }

        //=========================================================================================================

        private void buttonStart_Click(object sender, RoutedEventArgs e) {
            if (!UpdateTestParamsFromUI()) {
                return;
            }

            PreparePcmData();

            lock (mLock) {
                int hr = 0;

                hr = mWasapiPlay.Setup(listBoxPlayDevices.SelectedIndex,
                        WasapiCS.DeviceType.Play, WasapiCS.StreamType.PCM,
                        mSampleRate, mPlaySampleFormat, NUM_CHANNELS,
                        WasapiCS.MMCSSCallType.Enable, WasapiCS.MMThreadPriorityType.None,
                        WasapiCS.SchedulerTaskType.ProAudio, WasapiCS.ShareMode.Exclusive,
                        mPlayDataFeedMode, mPlayBufferMillisec, 1000, 10000);
                if (hr < 0) {
                    MessageBox.Show(string.Format(Properties.Resources.msgPlaySetupError,
                            mSampleRate, mPlaySampleFormat, NUM_CHANNELS, mPlayDataFeedMode, mPlayBufferMillisec));
                    mWasapiPlay.Unsetup();
                    return;
                }

                var ss = mWasapiPlay.GetSessionStatus();

                {
                    var data = mPcmSync.GetSampleArray();
                    var trimmed = new byte[ss.EndpointBufferFrameNum * mPcmSync.BitsPerFrame/8];
                    Array.Copy(data, trimmed,trimmed.Length);
                    mPcmSync.SetSampleArray(ss.EndpointBufferFrameNum, trimmed);
                }
                {
                    var data = mPcmReady.GetSampleArray();
                    var trimmed = new byte[ss.EndpointBufferFrameNum * mPcmReady.BitsPerFrame/8];
                    Array.Copy(data, trimmed,trimmed.Length);
                    mPcmReady.SetSampleArray(ss.EndpointBufferFrameNum, trimmed);
                }
                mWasapiPlay.ClearPlayList();
                mWasapiPlay.AddPlayPcmDataStart();
                mWasapiPlay.AddPlayPcmData(0, mPcmSync.GetSampleArray());
                mWasapiPlay.AddPlayPcmData(1, mPcmReady.GetSampleArray());
                mWasapiPlay.AddPlayPcmData(2, mPcmTest.GetSampleArray());
                mWasapiPlay.AddPlayPcmDataEnd();

                mWasapiPlay.SetPlayRepeat(false);
                mWasapiPlay.ConnectPcmDataNext(0, 0);

                hr = mWasapiPlay.StartPlayback(0);
                mPlayWorker.RunWorkerAsync();

                // 録音
                mCapturedBytes = 0;

                hr = mWasapiRec.Setup(listBoxRecDevices.SelectedIndex,
                        WasapiCS.DeviceType.Rec, WasapiCS.StreamType.PCM,
                        mSampleRate, mRecSampleFormat, NUM_CHANNELS,
                        WasapiCS.MMCSSCallType.Enable, WasapiCS.MMThreadPriorityType.None,
                        WasapiCS.SchedulerTaskType.ProAudio, WasapiCS.ShareMode.Exclusive,
                        mRecDataFeedMode, mRecBufferMillisec, 1000, 10000);
                if (hr < 0) {
                    MessageBox.Show(string.Format(Properties.Resources.msgRecSetupError,
                            mSampleRate, mRecSampleFormat, NUM_CHANNELS, mRecDataFeedMode, mRecBufferMillisec));
                    StopUnsetup();
                    return;
                }

                var playAttr = mWasapiPlay.GetDeviceAttributes(listBoxPlayDevices.SelectedIndex);
                var recAttr = mWasapiRec.GetDeviceAttributes(listBoxRecDevices.SelectedIndex);

                textBoxLog.Text += string.Format(Properties.Resources.msgTestStarted, mSampleRate, mNumTestFrames / mSampleRate);
                textBoxLog.Text += string.Format(Properties.Resources.msgPlaySettings,
                        mPlaySampleFormat, mPlayBufferMillisec, mPlayDataFeedMode, playAttr.Name);
                textBoxLog.Text += string.Format(Properties.Resources.msgRecSettings,
                        mRecSampleFormat, mRecBufferMillisec, mRecDataFeedMode, recAttr.Name);
                textBoxLog.ScrollToEnd();

                groupBoxPcmDataSettings.IsEnabled = false;
                groupBoxPlayback.IsEnabled = false;
                groupBoxRecording.IsEnabled = false;

                buttonStart.IsEnabled = false;
                buttonStop.IsEnabled = true;

                // SYNC失敗タイマーのセット
                mSyncTimeout.Start();

                hr = mWasapiRec.StartRecording();
                mRecWorker.RunWorkerAsync();

                mState = State.Syncing;
            }
        }

        void SyncTimeoutTickCallback(object sender, EventArgs e) {
            mSyncTimeout.Stop();
            textBoxLog.Text += Properties.Resources.msgSyncError;
            textBoxLog.ScrollToEnd();
            AbortTest();
        }

        private void StopUnsetup() {
            StopBlocking();
            mWasapiPlay.Unsetup();
            mWasapiRec.Unsetup();
        }

        private void AbortTest() {
            StopUnsetup();

            if (0 <= listBoxPlayDevices.SelectedIndex &&
                    0 <= listBoxRecDevices.SelectedIndex) {
                buttonStart.IsEnabled = true;
            } else {
                buttonStart.IsEnabled = false;
            }
            buttonStop.IsEnabled = false;

            groupBoxPcmDataSettings.IsEnabled = true;
            groupBoxPlayback.IsEnabled = true;
            groupBoxRecording.IsEnabled = true;

            progressBar1.Value = 0;
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e) {
            AbortTest();
        }

        private void CaptureSync(byte[] data) {
            int useBitsPerSample = WasapiCS.SampleFormatTypeToUseBitsPerSample(mRecSampleFormat) / 8;
            int nFrames = (int)(data.Length / useBitsPerSample / NUM_CHANNELS);
            int mRecSyncPosInBytes = -1;
            int zeroSamples = 0;
            int syncSamples = 0;
            for (int pos=0; pos < data.Length; pos += useBitsPerSample) {
                switch (mRecSampleFormat) {
                case WasapiCS.SampleFormatType.Sint16:
                    if (data[pos] == 0 && data[pos + 1] == 0) {
                        ++zeroSamples;
                    }
                    if (data[pos] == 4 && data[pos + 1] == 0) {
                        ++syncSamples;
                        mRecSyncPosInBytes = pos;
                    }
                    break;
                case WasapiCS.SampleFormatType.Sint24:
                    if (data[pos] == 0 && data[pos + 1] == 0 && data[pos + 2] == 0) {
                        ++zeroSamples;
                    }
                    if (data[pos] == 4 && data[pos + 1] == 0 && data[pos + 2] == 0) {
                        ++syncSamples;
                        mRecSyncPosInBytes = pos;
                    }
                    break;
                case WasapiCS.SampleFormatType.Sint32V24:
                    if (data[pos + 1] == 0 && data[pos + 2] == 0 && data[pos + 3] == 0) {
                        ++zeroSamples;
                    }
                    if (data[pos + 1] == 4 && data[pos + 2] == 0 && data[pos + 3] == 0) {
                        ++syncSamples;
                        mRecSyncPosInBytes = pos;
                    }
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
                }
            }
            if (0 <= mRecSyncPosInBytes && zeroSamples + syncSamples == nFrames * NUM_CHANNELS) {
                // SYNC frame arrived
                mSyncTimeout.Stop();

                //Console.WriteLine("Sync Frame arrived. offset={0}", mRecSyncPosInBytes);

                Array.Copy(data, mRecSyncPosInBytes, mCapturedPcmData, 0, data.Length - mRecSyncPosInBytes);
                mCapturedBytes = data.Length - mRecSyncPosInBytes;

                mWasapiPlay.ConnectPcmDataNext(0, 1);
                mState = State.Running;
            }
        }

        private void CaptureRunning(byte[] data) {
            if (mCapturedBytes + data.Length <= mCapturedPcmData.Length) {
                Array.Copy(data, 0, mCapturedPcmData, mCapturedBytes, data.Length);
                mCapturedBytes += data.Length;

                int capturedFrames = mCapturedBytes / NUM_CHANNELS / (WasapiCS.SampleFormatTypeToUseBitsPerSample(mRecSampleFormat) / 8);

                //Console.WriteLine("Captured {0} frames", capturedFrames);
            } else {
                // キャプチャー終了. データの整合性チェックはRecRunWorkerCompletedで行う。
                mState = State.RecCompleted;
            }
        }

        private void CompareRecordedData() {
            if (mState != State.RecCompleted) {
                textBoxLog.Text += Properties.Resources.msgCompareCaptureTooSmall;
                textBoxLog.ScrollToEnd();
                return;
            }
            textBoxLog.Text += Properties.Resources.msgCompareStarted;

            mPcmRecorded = new PcmDataLib.PcmData();
            mPcmRecorded.SetFormat(NUM_CHANNELS,
                    WasapiCS.SampleFormatTypeToUseBitsPerSample(mRecSampleFormat),
                    WasapiCS.SampleFormatTypeToValidBitsPerSample(mRecSampleFormat),
                    mSampleRate, PcmDataLib.PcmData.ValueRepresentationType.SInt,
                    mCapturedPcmData.Length / NUM_CHANNELS / (WasapiCS.SampleFormatTypeToUseBitsPerSample(mRecSampleFormat) / 8));
            mPcmRecorded.SetSampleArray(mCapturedPcmData);

            // 開始合図位置compareStartFrameをサーチ
            int compareStartFrame = -1;
            switch (mRecSampleFormat) {
            case WasapiCS.SampleFormatType.Sint16:
                for (int pos=0; pos < mPcmRecorded.NumFrames; ++pos) {
                    if (0x00030000 == mPcmRecorded.GetSampleValueInInt32(0, pos)) {
                        compareStartFrame = pos;
                        break;
                    }
                }
                break;
            case WasapiCS.SampleFormatType.Sint24:
            case WasapiCS.SampleFormatType.Sint32V24:
                for (int pos=0; pos < mPcmRecorded.NumFrames; ++pos) {
                    if (0x00000300 == mPcmRecorded.GetSampleValueInInt32(0, pos)) {
                        compareStartFrame = pos;
                        break;
                    }
                }
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
            if (compareStartFrame < 0) {
                textBoxLog.Text += Properties.Resources.msgCompareStartNotFound;
                textBoxLog.ScrollToEnd();
                return;
            }

            compareStartFrame += (int)mPcmReady.NumFrames;

            if (mPcmRecorded.NumFrames - compareStartFrame < mNumTestFrames) {
                textBoxLog.Text += Properties.Resources.msgCompareCaptureTooSmall;
                textBoxLog.ScrollToEnd();
                return;
            }

            // 送信データmPcmTestと受信データmPcmRecordedを比較
            int numTestBytes = mNumTestFrames * NUM_CHANNELS
                * (WasapiCS.SampleFormatTypeToValidBitsPerSample(mRecSampleFormat) / 8);

            for (int pos=0; pos < mNumTestFrames; ++pos) {
                for (int ch=0; ch<NUM_CHANNELS; ++ch) {
                    if (mPcmTest.GetSampleValueInInt32(ch, pos)
                            != mPcmRecorded.GetSampleValueInInt32(ch, pos + compareStartFrame)) {
                        textBoxLog.Text += string.Format(Properties.Resources.msgCompareDifferent,
                                numTestBytes / 1024 / 1024, numTestBytes * 8L / 1000 / 1000, mNumTestFrames / mSampleRate);
                        textBoxLog.ScrollToEnd();
                        return;
                    }
                }
            }

            textBoxLog.Text += string.Format(Properties.Resources.msgCompareIdentical,
                    numTestBytes / 1024 / 1024, numTestBytes * 8L / 1000 / 1000, mNumTestFrames / mSampleRate);
            textBoxLog.ScrollToEnd();
        }

        private void CaptureDataArrived(byte[] data) {
            lock (mLock) {
                // Console.WriteLine("CaptureDataArrived {0} bytes", data.Length);
                switch (mState) {
                case State.Syncing:
                    CaptureSync(data);
                    break;
                case State.Running:
                    CaptureRunning(data);
                    break;
                default:
                    break;
                }
            }
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e) {

        }

    }
}

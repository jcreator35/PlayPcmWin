using System;
using System.Windows;
using Wasapi;
using System.ComponentModel;
using System.Windows.Threading;
using System.Text;
using System.Threading;
using System.IO;
using WWUtil;

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

        private bool mInitialized = false;

        private WasapiCS mWasapiPlay;
        private WasapiCS mWasapiRec;

        private BackgroundWorker mPlayWorker;
        private BackgroundWorker mRecWorker;

        private Wasapi.WasapiCS.CaptureCallback mCaptureDataArrivedDelegate;

        private static int NUM_PROLOGUE_FRAMES = 262144;
        private long mNumTestFrames = 1000 * 1000;
        private static int NUM_CHANNELS = 2;
        private int mSampleRate;
        private WasapiCS.SampleFormatType mPlaySampleFormat;
        private WasapiCS.SampleFormatType mRecSampleFormat;
        private WasapiCS.DataFeedMode mPlayDataFeedMode;
        private WasapiCS.DataFeedMode mRecDataFeedMode;
        private int mPlayBufferMillisec;
        private int mRecBufferMillisec;
        private int mRecDwChannelMask;
        private int mPlayDeviceIdx = -1;
        private int mRecDeviceIdx = -1;
        private bool mUseFile = false;
        private int ZERO_FLUSH_MILLISEC = 1000;
        private int TIME_PERIOD = 10000;

        private DispatcherTimer mSyncTimeout;

        private State mState = State.Init;

        private PcmDataLib.PcmData mPcmSync;
        private PcmDataLib.PcmData mPcmReady;
        private PcmDataLib.PcmData mPcmTest;
        private PcmDataLib.PcmData mPcmRecorded;

        Random mRand = new Random();

        private LargeArray<byte> mCapturedPcmData;
        private long mCapturedBytes;

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

            mInitialized = true;
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

                if (listBoxPlayDevices.SelectedIndex < 0 && 0 < listBoxPlayDevices.Items.Count) {
                    listBoxPlayDevices.SelectedIndex = 0;
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

                if (listBoxRecDevices.SelectedIndex < 0 && 0 < listBoxRecDevices.Items.Count) {
                    listBoxRecDevices.SelectedIndex = 0;
                }
            }

            UpdateButtonStartStop(ButtonStartStopState.StartEnable);
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

            mPlayDeviceIdx = listBoxPlayDevices.SelectedIndex;
            mRecDeviceIdx = listBoxRecDevices.SelectedIndex;
            mUseFile = radioButtonPcmFile.IsChecked == true;

            int testFramesMbytes = -1;
            if (!Int32.TryParse(textBoxTestFrames.Text, out testFramesMbytes) || testFramesMbytes <= 0) {
                MessageBox.Show(Properties.Resources.msgPcmSizeError);
                return false;
            }
            mNumTestFrames = 1000L * 1000L * (long)testFramesMbytes;

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

            mRecDwChannelMask = 0;
            if (checkBoxRecSetDwChannelMask.IsChecked == true) {
                mRecDwChannelMask = WasapiCS.GetTypicalChannelMask(NUM_CHANNELS);
            }

            return true;
        }

        private void PreparePcmData() {
            {
                mPcmSync = new PcmDataLib.PcmData();
                mPcmSync.SetFormat(NUM_CHANNELS,
                        WasapiCS.SampleFormatTypeToUseBitsPerSample(mPlaySampleFormat),
                        WasapiCS.SampleFormatTypeToValidBitsPerSample(mPlaySampleFormat),
                        mSampleRate,
                        PcmDataLib.PcmData.ValueRepresentationType.SInt, mSampleRate);
                var syncData = new LargeArray<byte>((WasapiCS.SampleFormatTypeToUseBitsPerSample(mPlaySampleFormat) / 8) * NUM_CHANNELS * mPcmSync.NumFrames);
                mPcmSync.SetSampleLargeArray(syncData);
            }

            {
                mPcmReady = new PcmDataLib.PcmData();
                mPcmReady.CopyFrom(mPcmSync);
                var readyData = new LargeArray<byte>((WasapiCS.SampleFormatTypeToUseBitsPerSample(mPlaySampleFormat) / 8) * NUM_CHANNELS * mPcmSync.NumFrames);
                mPcmReady.SetSampleLargeArray(readyData);
            }

            if (mUseFile) {
                var conv = new WasapiPcmUtil.PcmFormatConverter(NUM_CHANNELS);
                mPcmTest = conv.Convert(mPlayPcmData, mPlaySampleFormat,
                    new WasapiPcmUtil.PcmFormatConverter.BitsPerSampleConvArgs(WasapiPcmUtil.NoiseShapingType.None));
                mNumTestFrames = mPcmTest.NumFrames;
            } else {
                mPcmTest = new PcmDataLib.PcmData();
                mPcmTest.CopyHeaderInfoFrom(mPcmSync);
                var randData = new LargeArray<byte>((long)(WasapiCS.SampleFormatTypeToUseBitsPerSample(mPlaySampleFormat) / 8) * NUM_CHANNELS * mNumTestFrames);
                var fragment = new byte[4096];

                for (long i = 0; i < randData.LongLength; i += fragment.Length) {
                    long count = fragment.Length;
                    if (randData.LongLength < i + count) {
                        count = randData.LongLength - i;
                    }

                    mRand.NextBytes(fragment);
                    randData.CopyFrom(fragment, 0, i, (int)count);
                }
                mPcmTest.SetSampleLargeArray(mNumTestFrames, randData);
            }

            mCapturedPcmData = new LargeArray<byte>((long)(WasapiCS.SampleFormatTypeToUseBitsPerSample(mRecSampleFormat) / 8)
                    * NUM_CHANNELS * (mNumTestFrames + NUM_PROLOGUE_FRAMES));

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
                UpdateButtonStartStop(ButtonStartStopState.StartEnable);

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

            groupBoxPcmDataSettings.IsEnabled = false;
            groupBoxPlayback.IsEnabled = false;
            groupBoxRecording.IsEnabled = false;

            UpdateButtonStartStop(ButtonStartStopState.Disable);

            textBoxLog.Text += "Preparing data.\n";
            textBoxLog.ScrollToEnd();

            mBwStartTesting = new BackgroundWorker();
            mBwStartTesting.DoWork += new DoWorkEventHandler(BwStartTesting_DoWork);
            mBwStartTesting.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BwStartTesting_RunWorkerCompleted);
            mBwStartTesting.RunWorkerAsync();
        }

        class StartTestingResult {
            public bool result;
            public string text;
        };

        void BwStartTesting_DoWork(object sender, DoWorkEventArgs e) {
            var r = new StartTestingResult();
            r.result = false;
            r.text = "StartTesting failed!\n";
            e.Result = r;

            PreparePcmData();

            System.GC.Collect();

            lock (mLock) {
                int hr = 0;

                int playDwChannelMask = WasapiCS.GetTypicalChannelMask(NUM_CHANNELS);
                hr = mWasapiPlay.Setup(mPlayDeviceIdx,
                        WasapiCS.DeviceType.Play, WasapiCS.StreamType.PCM,
                        mSampleRate, mPlaySampleFormat, NUM_CHANNELS, playDwChannelMask,
                        WasapiCS.MMCSSCallType.Enable, WasapiCS.MMThreadPriorityType.None,
                        WasapiCS.SchedulerTaskType.ProAudio, WasapiCS.ShareMode.Exclusive,
                        mPlayDataFeedMode, mPlayBufferMillisec, ZERO_FLUSH_MILLISEC, TIME_PERIOD);
                if (hr < 0) {
                    mWasapiPlay.Unsetup();
                    r.result = false;
                    r.text = string.Format(Properties.Resources.msgPlaySetupError,
                            mSampleRate, mPlaySampleFormat, NUM_CHANNELS, mPlayDataFeedMode, mPlayBufferMillisec);
                    e.Result = r;
                    return;
                }

                var ss = mWasapiPlay.GetSessionStatus();

                {
                    var data = mPcmSync.GetSampleLargeArray();
                    var trimmed = new LargeArray<byte>(ss.EndpointBufferFrameNum * mPcmSync.BitsPerFrame / 8);
                    trimmed.CopyFrom(data, 0, 0, trimmed.LongLength);
                    mPcmSync.SetSampleLargeArray(ss.EndpointBufferFrameNum, trimmed);
                }
                {
                    var data = mPcmReady.GetSampleLargeArray();
                    var trimmed = new LargeArray<byte>(ss.EndpointBufferFrameNum * mPcmReady.BitsPerFrame / 8);
                    trimmed.CopyFrom(data, 0, 0, trimmed.LongLength);
                    mPcmReady.SetSampleLargeArray(ss.EndpointBufferFrameNum, trimmed);
                }
                mWasapiPlay.ClearPlayList();
                mWasapiPlay.AddPlayPcmDataStart();
                mWasapiPlay.AddPlayPcmData(0, mPcmSync.GetSampleLargeArray());
                mWasapiPlay.AddPlayPcmData(1, mPcmReady.GetSampleLargeArray());
                mWasapiPlay.AddPlayPcmData(2, mPcmTest.GetSampleLargeArray());
                mWasapiPlay.AddPlayPcmDataEnd();

                mWasapiPlay.SetPlayRepeat(false);
                mWasapiPlay.ConnectPcmDataNext(0, 0);

                // 録音
                mCapturedBytes = 0;

                hr = mWasapiRec.Setup(mRecDeviceIdx,
                        WasapiCS.DeviceType.Rec, WasapiCS.StreamType.PCM,
                        mSampleRate, mRecSampleFormat, NUM_CHANNELS, mRecDwChannelMask,
                        WasapiCS.MMCSSCallType.Enable, WasapiCS.MMThreadPriorityType.None,
                        WasapiCS.SchedulerTaskType.ProAudio, WasapiCS.ShareMode.Exclusive,
                        mRecDataFeedMode, mRecBufferMillisec, ZERO_FLUSH_MILLISEC, TIME_PERIOD);
                if (hr < 0) {
                    r.result = false;
                    r.text = string.Format(Properties.Resources.msgRecSetupError,
                            mSampleRate, mRecSampleFormat, NUM_CHANNELS, mRecDataFeedMode, mRecBufferMillisec);
                    e.Result = r;
                    StopUnsetup();
                    return;
                }

                var playAttr = mWasapiPlay.GetDeviceAttributes(mPlayDeviceIdx);
                var recAttr = mWasapiRec.GetDeviceAttributes(mRecDeviceIdx);

                r.result = true;
                r.text = string.Format(Properties.Resources.msgTestStarted, mSampleRate, mNumTestFrames / mSampleRate, mNumTestFrames * 0.001 * 0.001);
                r.text += string.Format(Properties.Resources.msgPlaySettings,
                        mPlaySampleFormat, mPlayBufferMillisec, mPlayDataFeedMode, playAttr.Name);
                r.text += string.Format(Properties.Resources.msgRecSettings,
                        mRecSampleFormat, mRecBufferMillisec, mRecDataFeedMode, recAttr.Name);
                e.Result = r;
            }
        }

        void BwStartTesting_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            var r = e.Result as StartTestingResult;

            textBoxLog.Text += r.text;
            textBoxLog.ScrollToEnd();

            if (r.result == false) {
                // 失敗。
                groupBoxPcmDataSettings.IsEnabled = true;
                groupBoxPlayback.IsEnabled = true;
                groupBoxRecording.IsEnabled = true;

                UpdateButtonStartStop(ButtonStartStopState.StartEnable);
                return;
            }

            // 成功。
            UpdateButtonStartStop(ButtonStartStopState.StopEnable);

            System.GC.Collect();
            System.Threading.Thread.Sleep(500);

            // SYNC失敗タイマーのセット
            mSyncTimeout.Start();

            int hr = mWasapiPlay.StartPlayback(0);
            mPlayWorker.RunWorkerAsync();

            hr = mWasapiRec.StartRecording();
            mRecWorker.RunWorkerAsync();

            mState = State.Syncing;
        }

        BackgroundWorker mBwStartTesting;


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

        enum ButtonStartStopState {
            Disable,
            StartEnable,
            StopEnable,
        };

        private void UpdateButtonStartStop(ButtonStartStopState s) {
            switch (s) {
            case ButtonStartStopState.StartEnable:
                if (0 <= listBoxPlayDevices.SelectedIndex &&
                        0 <= listBoxRecDevices.SelectedIndex) {
                    buttonStart.IsEnabled = true;
                } else {
                    buttonStart.IsEnabled = false;
                }
                buttonStop.IsEnabled = false;
                break;
            case ButtonStartStopState.StopEnable:
                buttonStart.IsEnabled = false;
                buttonStop.IsEnabled = true;
                break;
            case ButtonStartStopState.Disable:
                buttonStart.IsEnabled = false;
                buttonStop.IsEnabled = false;
                break;
            }
        }

        private void AbortTest() {
            StopUnsetup();

            UpdateButtonStartStop(ButtonStartStopState.StartEnable);

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

                mCapturedPcmData.CopyFrom(data, mRecSyncPosInBytes, 0, data.Length - mRecSyncPosInBytes);
                mCapturedBytes = data.Length - mRecSyncPosInBytes;

                mWasapiPlay.ConnectPcmDataNext(0, 1);
                mState = State.Running;
            }
        }

        private void CaptureRunning(byte[] data) {
            if (mCapturedBytes + data.Length <= mCapturedPcmData.LongLength) {
                mCapturedPcmData.CopyFrom(data, 0, mCapturedBytes, data.Length);
                mCapturedBytes += data.Length;

                long capturedFrames = mCapturedBytes / NUM_CHANNELS / (WasapiCS.SampleFormatTypeToUseBitsPerSample(mRecSampleFormat) / 8);

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
                    mCapturedPcmData.LongLength / NUM_CHANNELS / (WasapiCS.SampleFormatTypeToUseBitsPerSample(mRecSampleFormat) / 8));
            mPcmRecorded.SetSampleLargeArray(mCapturedPcmData);

            // 開始合図位置compareStartFrameをサーチ
            long compareStartFrame = -1;
            switch (mRecSampleFormat) {
            case WasapiCS.SampleFormatType.Sint16:
                for (long pos=0; pos < mPcmRecorded.NumFrames; ++pos) {
                    if (0x00030000 == mPcmRecorded.GetSampleValueInInt32(0, pos)) {
                        compareStartFrame = pos;
                        break;
                    }
                }
                break;
            case WasapiCS.SampleFormatType.Sint24:
            case WasapiCS.SampleFormatType.Sint32V24:
                for (long pos=0; pos < mPcmRecorded.NumFrames; ++pos) {
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

            compareStartFrame += mPcmReady.NumFrames;

            if (mPcmRecorded.NumFrames - compareStartFrame < mNumTestFrames) {
                textBoxLog.Text += Properties.Resources.msgCompareCaptureTooSmall;
                textBoxLog.ScrollToEnd();
                return;
            }

            // 送信データmPcmTestと受信データmPcmRecordedを比較
            long numTestBytes = mNumTestFrames * NUM_CHANNELS
                * (WasapiCS.SampleFormatTypeToValidBitsPerSample(mRecSampleFormat) / 8);

            for (long pos=0; pos < mNumTestFrames; ++pos) {
                for (int ch=0; ch<NUM_CHANNELS; ++ch) {
                    if (mPcmTest.GetSampleValueInInt32(ch, pos)
                            != mPcmRecorded.GetSampleValueInInt32(ch, pos + compareStartFrame)) {
                        textBoxLog.Text += string.Format(Properties.Resources.msgCompareDifferent,
                                (double)numTestBytes /1024.0/1024.0, (double)numTestBytes * 8L * 0.001 * 0.001, (double)mNumTestFrames / mSampleRate);
                        textBoxLog.ScrollToEnd();
                        return;
                    }
                }
            }

            textBoxLog.Text += string.Format(Properties.Resources.msgCompareIdentical,
                    (double)numTestBytes /1024.0/1024.0, (double)numTestBytes * 8L * 0.001 * 0.001, (double)mNumTestFrames / mSampleRate);
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

        private BackgroundWorker mBwLoadPcm;

        private bool ReadPcmFile() {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Supported files|*.WAV;*.FLAC";

            Nullable<bool> result = dlg.ShowDialog();

            if (result != true) {
                return false;
            }

            groupBoxPcmDataSettings.IsEnabled = false;
            groupBoxPlayback.IsEnabled = false;
            groupBoxRecording.IsEnabled = false;

            UpdateButtonStartStop(ButtonStartStopState.Disable);

            textBoxLog.Text += string.Format("Reading {0} ... ", dlg.FileName);
            textBoxLog.ScrollToEnd();

            mBwLoadPcm = new BackgroundWorker();
            mBwLoadPcm.DoWork += new DoWorkEventHandler(LoadPcm_DoWork);
            mBwLoadPcm.RunWorkerCompleted += new RunWorkerCompletedEventHandler(LoadPcm_RunWorkerCompleted);
            mBwLoadPcm.RunWorkerAsync(dlg.FileName);

            return true;
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            if (!ReadPcmFile()) {
                radioButtonPcmRandom.IsChecked = true;
            }
        }

        private void radioButtonFile_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            if (mPlayPcmData != null) {
                return;
            }

            if (!ReadPcmFile()) {
                radioButtonPcmRandom.IsChecked = true;
            }
        }


        private PcmDataLib.PcmData mPlayPcmData;

        class LoadPcmResult {
            public string path;
            public bool result;
            public PcmDataLib.PcmData pcmData;
        };

        private void LoadPcm_DoWork(object sender, DoWorkEventArgs e) {
            string path = (string)e.Argument;
            var r = new LoadPcmResult();
            r.path = path;
            r.result = false;
            r.pcmData = null;

            mPlayPcmData = null;
            try {
                using (var br = new BinaryReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))) {
                    var reader = new WavRWLib2.WavReader();
                    if (reader.ReadHeaderAndSamples(br, 0, -1)
                        && reader.NumChannels == NUM_CHANNELS) {
                        var b = reader.GetSampleArray();
                        r.pcmData = new PcmDataLib.PcmData();
                        r.pcmData.SetFormat(NUM_CHANNELS, reader.BitsPerSample, reader.ValidBitsPerSample,
                            reader.SampleRate, reader.SampleValueRepresentationType, reader.NumFrames);
                        r.pcmData.SetSampleLargeArray(new LargeArray<byte>(b));
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine(ex);
                r.pcmData = null;
            }

            if (r.pcmData == null) {
                try {
                    var flacRW = new WWFlacRWCS.FlacRW();
                    int rv = flacRW.DecodeAll(r.path);
                    if (0 <= rv) {
                        WWFlacRWCS.Metadata metaData;
                        flacRW.GetDecodedMetadata(out metaData);
                        if (metaData.channels == NUM_CHANNELS) {
                            var pcmBytes = new LargeArray<byte>(metaData.PcmBytes);

                            int bytesPerSample = metaData.bitsPerSample/8;
                            var fragment = new byte[bytesPerSample];
                            for (long pos = 0; pos<metaData.totalSamples;++pos) {
                                for (int ch = 0; ch < NUM_CHANNELS; ++ch) {
                                    flacRW.GetDecodedPcmBytes(ch, pos * bytesPerSample, out fragment, bytesPerSample);
                                    pcmBytes.CopyFrom(fragment, 0, (long)bytesPerSample * (NUM_CHANNELS * pos + ch), bytesPerSample);
                                }
                            }

                            r.pcmData = new PcmDataLib.PcmData();
                            r.pcmData.SetFormat(NUM_CHANNELS, metaData.bitsPerSample, metaData.bitsPerSample,
                                metaData.sampleRate, PcmDataLib.PcmData.ValueRepresentationType.SInt, metaData.totalSamples);
                            r.pcmData.SetSampleLargeArray(pcmBytes);
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex);
                    r.pcmData = null;
                }
            }

            if (r.pcmData != null) {
                r.result = true;
            } else {
                r.result = false;
            }

            e.Result = r;
        }

        private void LoadPcm_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            var r = e.Result as LoadPcmResult;

            groupBoxPcmDataSettings.IsEnabled = true;
            groupBoxPlayback.IsEnabled = true;
            groupBoxRecording.IsEnabled = true;

            UpdateButtonStartStop(ButtonStartStopState.StartEnable);

            if (!r.result) {
                textBoxLog.Text += "Failed.\n";
                textBoxLog.ScrollToEnd();

                MessageBox.Show(string.Format("Error: Read failed. Only 2ch stereo WAV is supported: {0}",
                    r.path));
                radioButtonPcmRandom.IsChecked = true;
                return;
            }

            textBoxLog.Text += "Succeeded.\n";
            textBoxLog.ScrollToEnd();

            mPlayPcmData = r.pcmData;
            textBoxFile.Text = r.path;
            radioButtonPcmFile.IsChecked = true;
        }
    }
}

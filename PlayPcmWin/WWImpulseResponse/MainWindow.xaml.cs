using System;
using System.Windows;
using Wasapi;
using System.ComponentModel;
using System.Windows.Threading;
using System.Text;
using System.Threading;
using System.IO;
using WWUtil;
using System.Diagnostics;
using WWMath;
using System.Collections.Generic;

namespace WWImpulseResponse {
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

        private enum State {
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

        private int mSampleRate;
        private WasapiCS.SampleFormatType mPlaySampleFormat;
        private WasapiCS.SampleFormatType mRecSampleFormat;
        private WasapiCS.DataFeedMode mPlayDataFeedMode;
        private WasapiCS.DataFeedMode mRecDataFeedMode;
        private int mPlayBufferMillisec;
        private int mRecBufferMillisec;
        private int mPlayDeviceIdx = -1;
        private int mRecDeviceIdx = -1;
        private int ZERO_FLUSH_MILLISEC = 1000;
        private int TIME_PERIOD = 10000;

        private State mState = State.Init;

        private PcmDataLib.PcmData mPcmPlay;

        private Random mRand = new Random();

        private LargeArray<byte> mCapturedPcmData;
        private long mReceivedBytes;
        private long mCapturedBytes;

        private Object mLock = new Object();

        private Wasapi.WasapiCS.StateChangedCallback mStateChanged;

        private class StartTestingResult {
            public bool result;
            public string text;
        };

        private BackgroundWorker mBwStartTesting;

        private enum ButtonStartStopState {
            Disable,
            StartEnable,
            StopEnable,
        };

        private void LocalizeUI() {
        }

        private readonly int [] MLS_ORDERS = new int[] {
            16,
            17,
            18,
            19,
            20
        };
        
        private readonly int[] NUM_CH = new int[] {
            2,
            6,
            8
        };

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            LocalizeUI();

            mWasapiPlay = new WasapiCS();
            mWasapiPlay.Init();

            mWasapiRec = new WasapiCS();
            mWasapiRec.Init();
            mCaptureDataArrivedDelegate = new Wasapi.WasapiCS.CaptureCallback(CaptureDataArrived);
            mWasapiRec.RegisterCaptureCallback(mCaptureDataArrivedDelegate);

            mBwStartTesting = new BackgroundWorker();
            mBwStartTesting.DoWork += new DoWorkEventHandler(BwStartTesting_DoWork);
            mBwStartTesting.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BwStartTesting_RunWorkerCompleted);

            mPlayWorker = new BackgroundWorker();
            mPlayWorker.DoWork += new DoWorkEventHandler(PlayDoWork);
            mPlayWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PlayRunWorkerCompleted);
            mPlayWorker.WorkerSupportsCancellation = true;

            mRecWorker = new BackgroundWorker();
            mRecWorker.DoWork += new DoWorkEventHandler(RecDoWork);
            mRecWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(RecRunWorkerCompleted);
            mRecWorker.WorkerSupportsCancellation = true;
            mRecWorker.WorkerReportsProgress = true;
            mRecWorker.ProgressChanged += new ProgressChangedEventHandler(RecWorkerProgressChanged);


            UpdateDeviceList();

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
            if (radioButtonPlayPcm32v32.IsChecked == true) {
                mPlaySampleFormat = WasapiCS.SampleFormatType.Sint32;
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
            if (radioButtonRecPcm32v32.IsChecked == true) {
                mRecSampleFormat = WasapiCS.SampleFormatType.Sint32;
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

        //=========================================================================================================

        class StartTestingArgs {
            public int order;
            public int numChannels;
            public int testChannel;
            public int playDwChannelMask;
            public int recDwChannelMask;

            public StartTestingArgs(int aOrder, int numCh, int testCh, int playDwChMask, int recDwChMask) {
                order = aOrder;
                numChannels = numCh;
                testChannel = testCh;
                playDwChannelMask = playDwChMask;
                recDwChannelMask = recDwChMask;
            }
        };

        private StartTestingArgs mStartTestingArgs;

        // 開始ボタンを押すと以下の順に実行される。
        //                BwStartTesting_DoWork()
        //                └PreparePcmData()
        //                BwStartTesting_RunWorkerCompleted()
        //                     ↓                       ↓
        // mPlayWorker.RunWorkerAsync()         mRecWorker.RunWorkerAsync()
        // PlayDoWork()                         RecDoWork() → CaptureDataArrived()
        //   (リピート再生)                                    ├CaptureSync()
        //                                                    └CaptureRunning()
        // PlayRunWorkerCompleted()   ←──────────── mWasapiPlay.Stop()
        //                                          mWasapiRec.Stop()
        //                                          ProcessCapturedData()
        //                                      RecRunWorkerCompleted()
        private void buttonStart_Click(object sender, RoutedEventArgs e) {
            if (!UpdateTestParamsFromUI()) {
                return;
            }

            //Console.WriteLine("buttonStart_Click()");

            groupBoxPcmDataSettings.IsEnabled = false;
            groupBoxPlayback.IsEnabled = false;
            groupBoxRecording.IsEnabled = false;

            UpdateButtonStartStop(ButtonStartStopState.Disable);

            textBoxLog.Text += "Preparing data.\n";
            textBoxLog.ScrollToEnd();

            int numCh = NUM_CH[comboBoxNumChannels.SelectedIndex];

            int playDwChMask = WasapiCS.GetTypicalChannelMask(numCh);

            int recDwChMask = 0;
            if (checkBoxRecSetDwChannelMask.IsChecked == true) {
                recDwChMask = WasapiCS.GetTypicalChannelMask(numCh);
            }

            int testCh = 0;
            if (!Int32.TryParse(textBoxTestChannel.Text, out testCh) || testCh < 0 || numCh <= testCh) {
                MessageBox.Show("Error: test channel number is out of range");
                return;
            }
            mStartTestingArgs = new StartTestingArgs(MLS_ORDERS[comboBoxMLSOrder.SelectedIndex], numCh, testCh, playDwChMask, recDwChMask);
            mBwStartTesting.RunWorkerAsync(mStartTestingArgs);
        }

        private LargeArray<byte> CreatePcmData(byte[] mls, WasapiCS.SampleFormatType sft, int numCh) {
            int sampleBytes = (WasapiCS.SampleFormatTypeToUseBitsPerSample(sft)/8);
            int frameBytes = sampleBytes * numCh;
            long bytes = (long)mls.Length * frameBytes;
            var r = new LargeArray<byte>(bytes);

            long writePos = 0;
            for (long i = 0; i < mls.Length; ++i) {
                for (int ch=0; ch<numCh; ++ch) {
                    int v = 0x7fffffff;
                    
                    // -6dBする。
                    v /= 2;

                    if (mls[i] == 0) {
                        v = -v;
                    }

                    uint uV = (uint)v;

                    for (int c = 0; c<sampleBytes; ++c) {
                        byte b = (byte)(uV >> (8*(4 - sampleBytes + c)));
                        r.Set(writePos++, b);
                    }
                }
            }
            return r;
        }

        private void PreparePcmData(StartTestingArgs args) {
            var mls = new MaximumLengthSequence(args.order);
            var seq = mls.Sequence();

            // mPcmTest : テストデータ。このPCMデータを再生し、インパルス応答特性を調べる。
            mPcmPlay = new PcmDataLib.PcmData();
            mPcmPlay.SetFormat(args.numChannels,
                WasapiCS.SampleFormatTypeToUseBitsPerSample(mPlaySampleFormat),
                WasapiCS.SampleFormatTypeToValidBitsPerSample(mPlaySampleFormat),
                mSampleRate,
                PcmDataLib.PcmData.ValueRepresentationType.SInt, seq.Length);
            mPcmPlay.SetSampleLargeArray(CreatePcmData(seq, mPlaySampleFormat, args.numChannels));

            // 録音データ置き場。
            int recBytesPerSample = WasapiCS.SampleFormatTypeToUseBitsPerSample(mRecSampleFormat) / 8;
            mCapturedPcmData = new LargeArray<byte>((long)recBytesPerSample * args.numChannels * seq.Length);
        }

        private void BwStartTesting_DoWork(object sender, DoWorkEventArgs e) {
            //Console.WriteLine("BwStartTesting_DoWork()");
            var args = e.Argument as StartTestingArgs;
            var r = new StartTestingResult();
            r.result = false;
            r.text = "StartTesting failed!\n";
            e.Result = r;

            PreparePcmData(args);

            System.GC.Collect();

            lock (mLock) {
                int hr = 0;

                // 録音
                mCapturedBytes = 0;
                mReceivedBytes = 0;

                hr = mWasapiRec.Setup(mRecDeviceIdx,
                        WasapiCS.DeviceType.Rec, WasapiCS.StreamType.PCM,
                        mSampleRate, mRecSampleFormat, args.numChannels, args.recDwChannelMask,
                        WasapiCS.MMCSSCallType.Enable, WasapiCS.MMThreadPriorityType.None,
                        WasapiCS.SchedulerTaskType.ProAudio, WasapiCS.ShareMode.Exclusive,
                        mRecDataFeedMode, mRecBufferMillisec, ZERO_FLUSH_MILLISEC, TIME_PERIOD, true);
                if (hr < 0) {
                    r.result = false;
                    r.text = string.Format(Properties.Resources.msgRecSetupError,
                            mSampleRate, mRecSampleFormat, args.numChannels, mRecDataFeedMode,
                            mRecBufferMillisec, mWasapiRec.GetErrorMessage(hr)) + "\n";
                    e.Result = r;
                    StopUnsetup();
                    return;
                }

                // 再生

                hr = mWasapiPlay.Setup(mPlayDeviceIdx,
                        WasapiCS.DeviceType.Play, WasapiCS.StreamType.PCM,
                        mSampleRate, mPlaySampleFormat, args.numChannels, args.playDwChannelMask,
                        WasapiCS.MMCSSCallType.Enable, WasapiCS.MMThreadPriorityType.None,
                        WasapiCS.SchedulerTaskType.ProAudio, WasapiCS.ShareMode.Exclusive,
                        mPlayDataFeedMode, mPlayBufferMillisec, ZERO_FLUSH_MILLISEC, TIME_PERIOD, true);
                if (hr < 0) {
                    mWasapiPlay.Unsetup();
                    r.result = false;
                    r.text = string.Format(Properties.Resources.msgPlaySetupError,
                            mSampleRate, mPlaySampleFormat, args.numChannels, mPlayDataFeedMode, mPlayBufferMillisec) + "\n";
                    e.Result = r;
                    return;
                }

                var ss = mWasapiPlay.GetSessionStatus();

                mWasapiPlay.ClearPlayList();
                mWasapiPlay.AddPlayPcmDataStart();
                mWasapiPlay.AddPlayPcmData(0, mPcmPlay.GetSampleLargeArray());
                mWasapiPlay.AddPlayPcmDataEnd();

                mWasapiPlay.SetPlayRepeat(true);

                var playAttr = mWasapiPlay.GetDeviceAttributes(mPlayDeviceIdx);
                var recAttr = mWasapiRec.GetDeviceAttributes(mRecDeviceIdx);

                r.result = true;
                r.text = string.Format(Properties.Resources.msgTestStarted, mSampleRate, mPcmPlay.NumFrames / mSampleRate, mPcmPlay.NumFrames * 0.001 * 0.001);
                r.text += string.Format(Properties.Resources.msgPlaySettings,
                        mPlaySampleFormat, mPlayBufferMillisec, mPlayDataFeedMode, playAttr.Name);
                r.text += string.Format(Properties.Resources.msgRecSettings,
                        mRecSampleFormat, mRecBufferMillisec, mRecDataFeedMode, recAttr.Name);
                e.Result = r;
            }
        }

        void BwStartTesting_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            //Console.WriteLine("BwStartTesting_RunWorkerCompleted()");
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

            int hr = mWasapiPlay.StartPlayback(0);
            mPlayWorker.RunWorkerAsync();

            hr = mWasapiRec.StartRecording();
            mRecWorker.RunWorkerAsync();

            mState = State.Syncing;
        }

        /// <summary>
        /// 再生中。バックグラウンドスレッド。
        /// </summary>
        private void PlayDoWork(object o, DoWorkEventArgs args) {
            //Console.WriteLine("PlayDoWork started");
            BackgroundWorker bw = (BackgroundWorker)o;

            while (!mWasapiPlay.Run(100)) {
                //Console.WriteLine("PlayDoWork ");
                System.Threading.Thread.Sleep(1);
                if (bw.CancellationPending) {
                    Console.WriteLine("PlayDoWork() CANCELED");
                    mWasapiPlay.Stop();
                    args.Cancel = true;
                }
            }

            // 正常に最後まで再生が終わった場合、ここでStopを呼んで、後始末する。
            // キャンセルの場合は、2回Stopが呼ばれることになるが、問題ない!!!
            mWasapiPlay.Stop();

            // 停止完了後タスクの処理は、ここではなく、PlayRunWorkerCompletedで行う。
        }

        private void RecWorkerProgressChanged(object sender, ProgressChangedEventArgs e) {
            progressBar1.Value = e.ProgressPercentage;
        }

        /// <summary>
        /// 再生終了。
        /// </summary>
        private void PlayRunWorkerCompleted(object o, RunWorkerCompletedEventArgs args) {
            mWasapiPlay.Unsetup();
        }

        private void RecDoWork(object o, DoWorkEventArgs args) {
            BackgroundWorker bw = (BackgroundWorker)o;

            bw.ReportProgress(10);

            while (!mWasapiRec.Run(1000) && mState != State.RecCompleted) {
                if (mState == State.Running) {
                    bw.ReportProgress(25);
                }

                System.Threading.Thread.Sleep(1);
                if (bw.CancellationPending) {
                    Console.WriteLine("RecDoWork() CANCELED");
                    mWasapiRec.Stop();
                    args.Cancel = true;
                }
            }

            bw.ReportProgress(50);

            // 再生停止する。
            mWasapiPlay.Stop();

            lock (mLock) {
                mWasapiRec.Stop();
                mWasapiRec.Unsetup();
            }

            ProcessCapturedData();
        }

        private void ProcessCapturedData() {
            // 録音したデータをrecPcmDataに入れる。
            var recPcmData = new PcmDataLib.PcmData();
            recPcmData.SetFormat(mStartTestingArgs.numChannels, WasapiCS.SampleFormatTypeToUseBitsPerSample(mRecSampleFormat),
                WasapiCS.SampleFormatTypeToValidBitsPerSample(mRecSampleFormat),
                mSampleRate,
                PcmDataLib.PcmData.ValueRepresentationType.SInt, mPcmPlay.NumFrames);
            recPcmData.SetSampleLargeArray(mCapturedPcmData);

            // double型に変換。
            var a = mPcmPlay.GetDoubleArray(mStartTestingArgs.testChannel);
            var b = recPcmData.GetDoubleArray(mStartTestingArgs.testChannel);

            var c = CrossCorrelation.CalcCircularCrossCorrelation(
                a.ToArray(), b.ToArray());

            using (var sw = new StreamWriter(File.Open("outputA.csv", FileMode.Create))) {
                for (int i = 0; i < a.LongLength; ++i) {
                    sw.WriteLine("{0}", a.At(i));
                }
            }
            using (var sw = new StreamWriter(File.Open("outputB.csv", FileMode.Create))) {
                for (int i = 0; i < b.LongLength; ++i) {
                    sw.WriteLine("{0}", b.At(i));
                }
            }
            using (var sw = new StreamWriter(File.Open("outputC.csv", FileMode.Create))) {
                for (int i = 0; i < c.Length; ++i) {
                    sw.WriteLine("{0}", c[i]);
                }
            }
        }

        private void RecRunWorkerCompleted(object o, RunWorkerCompletedEventArgs args) {
            //Console.WriteLine("RecRunWorkerCompleted()");

            lock (mLock) {
                // 完了。UIの状態を戻す。
                UpdateButtonStartStop(ButtonStartStopState.StartEnable);

                groupBoxPcmDataSettings.IsEnabled = true;
                groupBoxPlayback.IsEnabled = true;
                groupBoxRecording.IsEnabled = true;

                progressBar1.Value = 0;

                mState = State.Init;
            }

            textBoxLog.Text += "Finished\n";
            textBoxLog.ScrollToEnd();

        }

        private void StopUnsetup() {
            StopBlocking();
            mWasapiPlay.Unsetup();
            mWasapiRec.Unsetup();
        }

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
            // 1再生ぶん待つ。
            mReceivedBytes += data.Length;
            if (mCapturedPcmData.LongLength <= mReceivedBytes) {
                
                mState = State.Running;
            }
        }

        private void CaptureRunning(byte[] data) {
            // 届いたPCMデータをmCapturedPcmDataにAppendし、
            // mCapturedBytesを更新する。
            if (mCapturedBytes + data.Length <= mCapturedPcmData.LongLength) {
                mCapturedPcmData.CopyFrom(data, 0, mCapturedBytes, data.Length);
                mCapturedBytes += data.Length;

                long capturedFrames = mCapturedBytes / mStartTestingArgs.numChannels / (WasapiCS.SampleFormatTypeToUseBitsPerSample(mRecSampleFormat) / 8);

                //Console.WriteLine("Captured {0} frames", capturedFrames);
            } else {
                int copyBytes = (int)(mCapturedPcmData.LongLength - mCapturedBytes);

                mCapturedPcmData.CopyFrom(data, 0, mCapturedBytes, copyBytes);
                mCapturedBytes += copyBytes;

                // キャプチャー終了. データの整合性チェックはRecRunWorkerCompletedで行う。
                mState = State.RecCompleted;
            }
        }

        private void CaptureDataArrived(byte[] data) {
            lock (mLock) {
                // Console.WriteLine("CaptureDataArrived {0} bytes, {1} frames", data.Length, data.Length / (mPcmTest.BitsPerFrame/8));
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

    }
}

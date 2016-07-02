using System;
using System.Text;
using System.Windows;
using Wasapi;
using WavRWLib2;
using System.IO;
using System.ComponentModel;
using System.Globalization;
using PcmDataLib;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media;

namespace RecPcmWin {
    public partial class MainWindow : Window {
        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        private long mLevelMeterLastDispTick = 0;
        private const long LEVEL_METER_UPDATE_INTERVAL_MS = 66;
        private const double METER_LEFT_X = 50.0;
        private const double METER_WIDTH = 400.0;
        private const double METER_0DB_W = 395.0;
        private const double METER_SMALLEST_DB = -48.0;

        private WasapiControl mWasapiCtrl = new WasapiControl();
        private Preference mPref = null;
        private BackgroundWorker mBW;
        private bool mInitialized = false;
        private List<WasapiCS.DeviceAttributes> mDeviceList = null;
        private LevelMeter mLevelMeter;

        private string [] mResourceCultureNameArray = new string[] {
            "cs-CZ",
            "en-US",
            "ja-JP",
        };

        private int CultureStringToIdx(string s) {
            int idx = 1; // US-English
            for (int i = 0; i < mResourceCultureNameArray.Length; ++i) {
                if (0 == string.CompareOrdinal(s, mResourceCultureNameArray[i])) {
                    idx = i;
                }
            }

            return idx;
        }

        public void AddLog(string text) {
            textBoxLog.Text += text;
            textBoxLog.ScrollToEnd();
        }

        public MainWindow() {
            InitializeComponent();

            mPref = PreferenceStore.Load();

            if (0 != string.CompareOrdinal(Thread.CurrentThread.CurrentUICulture.Name, mPref.CultureString)) {
                // カルチャーをセットする。
                CultureInfo newCulture = new CultureInfo(mPref.CultureString);
                Thread.CurrentThread.CurrentCulture = newCulture;
                Thread.CurrentThread.CurrentUICulture = newCulture;
            }

            comboBoxLang.SelectedIndex = CultureStringToIdx(Thread.CurrentThread.CurrentUICulture.Name);

            int hr = 0;
            hr = mWasapiCtrl.Init();
            AddLog(string.Format("RecPcmWin version {0}\r\n", AssemblyVersion));
            AddLog(string.Format("wasapi.Init() {0:X8}\r\n", hr));

            Closed += new EventHandler(MainWindow_Closed);

            CreateDeviceList();

            UpdateUITexts();

            PreferenceToUI();

            int currentSec = 0;
            int maxSec = (int)((long)mPref.RecordingBufferSizeMB * 1024 * 1024 / GetBytesPerSec(mPref));
            UpdateDurationLabel(currentSec, maxSec);
            
            ResetLevelMeter();

            mLevelMeter = new LevelMeter(mPref.SampleFormat, mPref.NumOfChannels, mPref.PeakHoldSeconds,
                mPref.WasapiBufferSizeMS * 0.001, mPref.ReleaseTimeDbPerSec);

            mInitialized = true;
        }

        private void UpdateUITexts() {
            groupBoxUISettings.Header = Properties.Resources.MainDisplaySettings;
            groupBoxWasapiSettings.Header = Properties.Resources.MainWasapiSettings;
            groupBoxSampleRate.Header = Properties.Resources.MainSampleRate;
            groupBoxDeviceSelect.Header = Properties.Resources.MainListOfRecordingDevices;
            groupBoxLog.Header = Properties.Resources.MainLog;
            groupBoxNumOfChannels.Header = Properties.Resources.MainNumOfChannels;
            groupBoxOperationMode.Header = Properties.Resources.MainOperationMode;
            groupBoxRecordingBufferSize.Header = Properties.Resources.MainRecordingDataSize;
            groupBoxWasapiBufferSize.Header = Properties.Resources.MainWasapiBufferSize;
            groupBoxRecordingControl.Header = Properties.Resources.MainRecordingControl;
            groupBoxQuantizationBitRate.Header = Properties.Resources.MainQuantizationBitRate;
            radioButtonEventDriven.Content = Properties.Resources.EventDriven;
            radioButtonTimerDriven.Content = Properties.Resources.TimerDriven;
            buttonInspectDevice.Content = Properties.Resources.MainAvailableFormats;
            buttonRec.Content = Properties.Resources.MainRecord;
            buttonStop.Content = Properties.Resources.MainStop;
            buttonSelectDevice.Content = Properties.Resources.MainSelect;
            buttonDeselectDevice.Content = Properties.Resources.MainDeselect;
            labelLanguage.Content = Properties.Resources.MainLanguage;
            groupBoxLevelMeter.Header = Properties.Resources.MainLevelMeter;
            groupBoxPeakHold.Header = Properties.Resources.MainPeakHold;
            groupBoxNominalPeakLevel.Header = Properties.Resources.MainNominalPeakLevel;
            groupBoxLevelMeterOther.Header = Properties.Resources.MainLevelMeterOther;
            checkBoxLevelMeterUpdateWhileRecording.Content = Properties.Resources.MainLevelMeterUpdateWhileRecording;
            labelLevelMeterReleaseTime.Content = Properties.Resources.MainLevelMeterReleaseTime;
            checkBoxSetDwChannelMask.Content = Properties.Resources.MainCheckboxSetDwChannelMask;
            groupBoxDwChannelMask.Header = Properties.Resources.MainGroupBoxDwChannelMask;
        }

        private void PreferenceToUI() {
            switch (mPref.SampleRate) {
            case 44100:
            default:
                radioButton44100.IsChecked = true;
                break;
            case 48000:
                radioButton48000.IsChecked = true;
                break;
            case 88200:
                radioButton88200.IsChecked = true;
                break;
            case 96000:
                radioButton96000.IsChecked = true;
                break;
            case 176400:
                radioButton176400.IsChecked = true;
                break;
            case 192000:
                radioButton192000.IsChecked = true;
                break;
            }

            switch (mPref.SampleFormat) {
            case WasapiCS.SampleFormatType.Sint16:
            default:
                radioButtonSint16.IsChecked = true;
                break;
            case WasapiCS.SampleFormatType.Sint24:
                radioButtonSint24.IsChecked = true;
                break;
            case WasapiCS.SampleFormatType.Sint32V24:
                radioButtonSint32v24.IsChecked = true;
                break;
            case WasapiCS.SampleFormatType.Sint32:
                radioButtonSint32.IsChecked = true;
                break;
            }

            if (mPref.NumOfChannels < 2) {
                mPref.NumOfChannels = 2;
            }
            textBoxNumOfChannels.Text = string.Format(
                    CultureInfo.InvariantCulture, "{0}",
                    mPref.NumOfChannels);

            if (mPref.WasapiBufferSizeMS < 3) {
                mPref.WasapiBufferSizeMS = 3;
            }
            textBoxWasapiBufferSizeMS.Text = string.Format(
                    CultureInfo.InvariantCulture, "{0}",
                    mPref.WasapiBufferSizeMS);

            switch (mPref.WasapiDataFeedMode) {
            case WasapiCS.DataFeedMode.EventDriven:
            default:
                radioButtonEventDriven.IsChecked = true;
                break;
            case WasapiCS.DataFeedMode.TimerDriven:
                radioButtonTimerDriven.IsChecked = true;
                break;
            }

            if (mPref.RecordingBufferSizeMB < 1) {
                mPref.RecordingBufferSizeMB = 1;
            }
            textBoxRecordingBufferSizeMB.Text = string.Format(
                CultureInfo.InvariantCulture, "{0}",
                mPref.RecordingBufferSizeMB);

            switch (mPref.PeakHoldSeconds) {
            case 1:
            default:
                radioButtonPeakHold1sec.IsChecked = true;
                break;
            case 3:
                radioButtonPeakHold3sec.IsChecked = true;
                break;
            case -1:
                radioButtonPeakHoldInfinity.IsChecked = true;
                break;
            }

            switch (mPref.YellowLevelDb) {
            case -6:
                radioButtonNominalPeakM6.IsChecked = true;
                break;
            case -10:
                radioButtonNominalPeakM10.IsChecked = true;
                break;
            case -12:
            default:
                radioButtonNominalPeakM12.IsChecked = true;
                break;
            }

            checkBoxLevelMeterUpdateWhileRecording.IsChecked = mPref.UpdateLevelMeterWhileRecording;

            checkBoxSetDwChannelMask.IsChecked = mPref.SetDwChannelMask;

            textBoxLevelMeterReleaseTime.Text = string.Format(
                CultureInfo.InvariantCulture, "{0}",
                mPref.ReleaseTimeDbPerSec);

            UpdateLevelMeterScale();
        }

        /// <summary>
        /// -48dBのとき0
        /// 0dBよりわずかに少ないとき390
        /// 0dB以上の時400
        /// </summary>
        private static double MeterValueDbToW(double db) {
            if (db < METER_SMALLEST_DB) {
                return 0;
            }
            if (0 <= db) {
                return METER_0DB_W;
            }

            return -(db / METER_SMALLEST_DB) * METER_0DB_W + METER_0DB_W;
        }

        private Brush DbToBrush(double dB) {
            if (dB < mPref.YellowLevelDb) {
                return new SolidColorBrush(Colors.Lime);
            }
            if (dB < -0.1) {
                return new SolidColorBrush(Colors.Yellow);
            }
            return new SolidColorBrush(Colors.Red);
        }

        private void UpdateLevelMeterScale() {
            double greenW = MeterValueDbToW(mPref.YellowLevelDb);
            rectangleGL.Width = greenW;
            rectangleGR.Width = greenW;
            Canvas.SetLeft(rectangleYL, METER_LEFT_X + greenW);
            Canvas.SetLeft(rectangleYR, METER_LEFT_X + greenW);
            rectangleYL.Width = METER_0DB_W - greenW;
            rectangleYR.Width = METER_0DB_W - greenW;
            Canvas.SetLeft(rectangleRL, METER_LEFT_X + METER_0DB_W);
            Canvas.SetLeft(rectangleRR, METER_LEFT_X + METER_0DB_W);

            switch (mPref.YellowLevelDb) {
            case -12:
            case -6:
                lineM10dB.Visibility = System.Windows.Visibility.Hidden;
                labelLevelMeterM10dB.Visibility = System.Windows.Visibility.Hidden;
                lineM12dB.Visibility = System.Windows.Visibility.Visible;
                labelLevelMeterM12dB.Visibility = System.Windows.Visibility.Visible;
                break;
            case -10:
                lineM10dB.Visibility = System.Windows.Visibility.Visible;
                labelLevelMeterM10dB.Visibility = System.Windows.Visibility.Visible;
                lineM12dB.Visibility = System.Windows.Visibility.Hidden;
                labelLevelMeterM12dB.Visibility = System.Windows.Visibility.Hidden;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
        }

        // 桁数を右揃えにして表示する。
        private static string DbToString(double db) {
            if (db < -200) {
                return "Low";
            }
            var s = string.Format(CultureInfo.CurrentCulture, "{0:+0.0;-0.0;+0.0}", db);
            return string.Format(CultureInfo.InvariantCulture, "{0,6}", s);
        }

        private void UpdateLevelMeter(double [] peakDb, double [] peakHoldDb) {
            switch (peakDb.Length) {
            case 2:
                {
                    double maskLW = METER_WIDTH - MeterValueDbToW(peakDb[0]);
                    double maskRW = METER_WIDTH - MeterValueDbToW(peakDb[1]);

                    Canvas.SetLeft(rectangleMaskL, METER_LEFT_X + (METER_WIDTH - maskLW));
                    Canvas.SetLeft(rectangleMaskR, METER_LEFT_X + (METER_WIDTH - maskRW));
                    rectangleMaskL.Width = maskLW;
                    rectangleMaskR.Width = maskRW;

                    double peakHoldLX = METER_LEFT_X + MeterValueDbToW(peakHoldDb[0]);
                    double peakHoldRX = METER_LEFT_X + MeterValueDbToW(peakHoldDb[1]);
                    Canvas.SetLeft(rectanglePeakL, peakHoldLX);
                    Canvas.SetLeft(rectanglePeakR, peakHoldRX);

                    rectanglePeakL.Fill = DbToBrush(peakHoldDb[0]);
                    rectanglePeakR.Fill = DbToBrush(peakHoldDb[1]);

                    textBoxLevelMeterL.Text = string.Format(CultureInfo.CurrentCulture, "  L\n{0}", DbToString(peakDb[0]));
                    textBoxLevelMeterR.Text = string.Format(CultureInfo.CurrentCulture, "  R\n{0}", DbToString(peakDb[1]));
                }
                break;
            default:
                break;
            }
        }

        private void ResetLevelMeter() {
            Canvas.SetLeft(rectangleMaskL, METER_LEFT_X);
            Canvas.SetLeft(rectangleMaskR, METER_LEFT_X);
            rectangleMaskL.Width = METER_WIDTH;
            rectangleMaskR.Width = METER_WIDTH;
            Canvas.SetLeft(rectanglePeakL, METER_LEFT_X);
            Canvas.SetLeft(rectanglePeakR, METER_LEFT_X);
            rectanglePeakL.Fill = new SolidColorBrush(Colors.Transparent);
            rectanglePeakR.Fill = new SolidColorBrush(Colors.Transparent);
            textBoxLevelMeterL.Text = string.Format(CultureInfo.CurrentCulture, "  L");
            textBoxLevelMeterR.Text = string.Format(CultureInfo.CurrentCulture, "  R");
        }

        private void ControlCaptureCallback(byte[] pcmData) {
            // このスレッドは描画できないので注意。

            mLevelMeter.Update(pcmData);
            double[] peakDb = new double[2];
            double[] peakHoldDb = new double[2];
            for (int ch = 0; ch < 2; ++ch) {
                peakDb[ch] = mLevelMeter.GetPeakDb(ch);
                peakHoldDb[ch] = mLevelMeter.GetPeakHoldDb(ch);
            }

            if (DateTime.Now.Ticks - mLevelMeterLastDispTick < LEVEL_METER_UPDATE_INTERVAL_MS * 10000) {
                return;
            }

            mLevelMeterLastDispTick = DateTime.Now.Ticks;

            Dispatcher.BeginInvoke(new Action(delegate() {
                // 描画スレッドで描画する。
                UpdateLevelMeter(peakDb, peakHoldDb);
            }));
        }

        private void CreateDeviceList() {
            int hr;

            int selectedIndex = -1;

            listBoxDevices.Items.Clear();

            mDeviceList = new List<WasapiCS.DeviceAttributes>();
            hr = mWasapiCtrl.EnumerateRecDeviceNames(mDeviceList);
            textBoxLog.Text += string.Format("wasapi.DoDeviceEnumeration(Rec) {0:X8}\r\n", hr);

            for (int i = 0; i < mDeviceList.Count; ++i) {
                var d = mDeviceList[i];
                listBoxDevices.Items.Add(d.Name);

                if (0 < mPref.PreferredDeviceIdString.Length
                        && 0 == string.CompareOrdinal(mPref.PreferredDeviceIdString, d.DeviceIdString)) {
                    // お気に入りデバイスを選択状態にする。
                    selectedIndex = i;
                }
            }

            buttonSelectDevice.IsEnabled     = mDeviceList.Count != 0;
            buttonDeselectDevice.IsEnabled   = false;
            buttonRec.IsEnabled              = false;
            buttonStop.IsEnabled             = false;
            groupBoxWasapiSettings.IsEnabled = true;
            buttonInspectDevice.IsEnabled    = mDeviceList.Count != 0;

            if (mDeviceList.Count == 0) {
                return;
            }

            if (selectedIndex < 0) {
                selectedIndex = 0;
            }
            listBoxDevices.SelectedIndex = selectedIndex;
        }

        void MainWindow_Closed(object sender, EventArgs e) {
            mWasapiCtrl.Term();

            // 設定ファイルを書き出す。
            PreferenceStore.Save(mPref);

            Application.Current.Shutdown(0);
        }

        private int DeviceSetup() {
            int hr;
            bool bRv;

            {   // read num of channels
                int numOfChannels;
                bRv = Int32.TryParse(textBoxNumOfChannels.Text, out numOfChannels);
                if (!bRv || numOfChannels <= 1) {
                    string s = Properties.Resources.ErrorNumChannels;
                    MessageBox.Show(s);
                    AddLog(s);
                    AddLog("\r\n");
                    return -1;
                }
                mPref.NumOfChannels = numOfChannels;
            }

            {   // read WASAPI buffer millisec from the textbox
                int wasapiBufferSizeMS = -1;
                bRv = Int32.TryParse(textBoxWasapiBufferSizeMS.Text, out wasapiBufferSizeMS);
                if (!bRv || wasapiBufferSizeMS <= 0) {
                    string s = Properties.Resources.ErrorWasapiBufferSize;
                    MessageBox.Show(s);
                    AddLog(s);
                    AddLog("\r\n");
                    return -1;
                }
                mPref.WasapiBufferSizeMS = wasapiBufferSizeMS;
            }

            {   // read recording buffer size
                int megaBytes = 0;
                bRv = Int32.TryParse(textBoxRecordingBufferSizeMB.Text, out megaBytes);
                if (megaBytes <= 0 || 2047 < megaBytes) {
                    string s = Properties.Resources.ErrorRecordingBufferSize;
                    MessageBox.Show(s);
                    AddLog(s);
                    AddLog("\r\n");
                    return -1;
                }
                mPref.RecordingBufferSizeMB = megaBytes;
            }

            int dwChannelMask = 0;
            if (mPref.SetDwChannelMask) {
                dwChannelMask = GetChannelMask(mPref.NumOfChannels);
            }

            if (!mWasapiCtrl.AllocateCaptureMemory(
                    1024 * 1024 * mPref.RecordingBufferSizeMB)) {
                string s = Properties.Resources.ErrorCouldNotAllocateMemory;
                MessageBox.Show(s);
                AddLog(s);
                AddLog("\r\n");
                return -1;
            }

            hr = mWasapiCtrl.Setup(listBoxDevices.SelectedIndex,
                mPref.WasapiDataFeedMode, mPref.WasapiBufferSizeMS,
                mPref.SampleRate, mPref.SampleFormat, mPref.NumOfChannels, dwChannelMask);
            {
                if (hr < 0) {
                    string s = string.Format("Error: wasapi.Setup({0}Hz, {1}, {2}ms, {3}, {4}ch, dwChannelMask={5})\r\nError code = {6:X8}\r\n",
                            mPref.SampleRate, mPref.SampleFormat,
                            mPref.WasapiBufferSizeMS, mPref.WasapiDataFeedMode,
                            mPref.NumOfChannels, dwChannelMask, hr);
                    MessageBox.Show(s);
                    AddLog(s);

                    AddLog("wasapi.Unsetup()\r\n");
                    mWasapiCtrl.Unsetup();
                    mWasapiCtrl.ReleaseCaptureMemory();
                    return hr;
                } else {
                    string s = string.Format("wasapi.Setup({0}Hz, {1}, {2}ms, {3}, {4}ch) Succeeded {5:X8}\r\n",
                            mPref.SampleRate, mPref.SampleFormat,
                            mPref.WasapiBufferSizeMS, mPref.WasapiDataFeedMode,
                            mPref.NumOfChannels, hr);
                    AddLog(s);
                }
            }

            mPref.PreferredDeviceIdString = mDeviceList[listBoxDevices.SelectedIndex].DeviceIdString;

            return 0;
        }

        /// numChannels to channelMask
        /// please refer this article https://msdn.microsoft.com/en-us/library/windows/hardware/dn653308%28v=vs.85%29.aspx
        private static int
        GetChannelMask(int numChannels) {
            int result = 0;

            switch (numChannels) {
            case 1:
                result = 0; // mono (unspecified)
                break;
            case 2:
                result = 3; // 2ch stereo (FL FR)
                break;
            case 4:
                result = 0x33; // 4ch matrix (FL FR BL BR)
                break;
            case 6:
                result = 0x3f; // 5.1 surround (FL FR FC LFE BL BR)
                break;
            case 8:
                result = 0x63f; // 7.1 surround (FL FR FC LFE BL BR SL SR)
                break;
            default:
                // 0 means we does not specify particular speaker locations.
                result = 0;
                break;
            }

            return result;
        }

        private void buttonInspectDevice_Click(object sender, RoutedEventArgs e) {
            bool bRv;

            {   // read num of channels
                int numOfChannels;
                bRv = Int32.TryParse(textBoxNumOfChannels.Text, out numOfChannels);
                if (!bRv || numOfChannels <= 1) {
                    string s = Properties.Resources.ErrorNumChannels;
                    MessageBox.Show(s);
                    AddLog(s);
                    AddLog("\r\n");
                    return;
                }
                mPref.NumOfChannels = numOfChannels;
            }

            int dwChannelMask = 0;
            if (checkBoxSetDwChannelMask.IsChecked == true) {
                dwChannelMask = GetChannelMask(mPref.NumOfChannels);
            }

            {
                string s = mWasapiCtrl.InspectDevice(listBoxDevices.SelectedIndex, dwChannelMask);
                AddLog(s);
            }
        }

        private int TotalFrames() {
            if (mPref.RecordingBufferSizeMB < 0) {
                return 0;
            }

            return mPref.RecordingBufferSizeMB * 1024 * 1024 / WasapiCS.SampleFormatTypeToUseBitsPerSample(mPref.SampleFormat) / mPref.NumOfChannels * 8;
        }

        private void buttonSelectDevice_Click(object sender, RoutedEventArgs e) {
            if (DeviceSetup() < 0) {
                return;
            }

            mLevelMeter = new LevelMeter(mPref.SampleFormat, mPref.NumOfChannels, mPref.PeakHoldSeconds,
                mPref.WasapiBufferSizeMS * 0.001, mPref.ReleaseTimeDbPerSec);
            mWasapiCtrl.SetCaptureCallback(ControlCaptureCallback);
            mWasapiCtrl.StorePcm(false);
            mWasapiCtrl.StartRecording();

            buttonSelectDevice.IsEnabled = false;
            buttonDeselectDevice.IsEnabled = true;
            buttonRec.IsEnabled = true;
            buttonStop.IsEnabled = false;
            buttonInspectDevice.IsEnabled = false;
            groupBoxWasapiSettings.IsEnabled = false;

            mBW = new BackgroundWorker();
            mBW.WorkerReportsProgress = true;
            mBW.DoWork += new DoWorkEventHandler(DoWork);
            mBW.ProgressChanged += new ProgressChangedEventHandler(ProgressChanged);
            mBW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(RunWorkerCompleted);
            mBW.RunWorkerAsync();
        }

        private void buttonDeselectDevice_Click(object sender, RoutedEventArgs e) {
            mWasapiCtrl.SetCaptureCallback(null);

            mWasapiCtrl.Stop();
            buttonDeselectDevice.IsEnabled = false;
            AddLog(string.Format("wasapi.Stop()\r\n"));
        }

        private void buttonRec_Click(object sender, RoutedEventArgs e) {
            if (checkBoxLevelMeterUpdateWhileRecording.IsChecked != true) {
                mWasapiCtrl.SetCaptureCallback(null);
                ResetLevelMeter();
            }

            AddLog(string.Format("StorePcm(true)\r\n"));
            mWasapiCtrl.StorePcm(true);

            slider1.Value = 0;
            slider1.Maximum = TotalFrames();
            buttonStop.IsEnabled     = true;
            buttonRec.IsEnabled      = false;
            buttonSelectDevice.IsEnabled = false;
            buttonDeselectDevice.IsEnabled = false;
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e) {
            mWasapiCtrl.Stop();
            buttonStop.IsEnabled = false;
            AddLog(string.Format("wasapi.Stop()\r\n"));
        }

        private void ProgressChanged(object o, ProgressChangedEventArgs args) {
            if (!mWasapiCtrl.IsRunning()) {
                return;
            }

            slider1.Value = mWasapiCtrl.GetPosFrame();

            double currentSec = (double)mWasapiCtrl.GetPosFrame() / mPref.SampleRate;
            double maxSec = (double)mWasapiCtrl.GetNumFrames() / mPref.SampleRate;
            UpdateDurationLabel((int)currentSec, (int)maxSec);

        }
        
        private static string SecondsToMSString(int seconds) {
            int m = seconds / 60;
            int s = seconds - m * 60;
            return string.Format(CultureInfo.CurrentCulture, "{0:D2}:{1:D2}", m, s);
        }

        private void UpdateDurationLabel(int currentSec, int maxSec) {
            labelDuration.Content = string.Format(CultureInfo.CurrentCulture, "{0:F1} / {1:F1}",
                SecondsToMSString(currentSec), SecondsToMSString(maxSec));
        }

        private void RunWorkerCompleted(object o, RunWorkerCompletedEventArgs args) {
            mWasapiCtrl.Stop();
            SaveRecordedData();

            AddLog("wasapi.Unsetup()\r\n");
            mWasapiCtrl.Unsetup();
            mWasapiCtrl.ReleaseCaptureMemory();

            RecordStopped();
        }

        private void RecordStopped() {
            buttonInspectDevice.IsEnabled = true;
            buttonSelectDevice.IsEnabled = true;
            buttonDeselectDevice.IsEnabled = false;
            buttonRec.IsEnabled = false;
            buttonStop.IsEnabled = false;
            groupBoxWasapiSettings.IsEnabled = true;
            ResetLevelMeter();
        }

        private void DoWork(object o, DoWorkEventArgs args) {
            while (!mWasapiCtrl.Run(200)) {
                mBW.ReportProgress(0);
                System.Threading.Thread.Sleep(1);
            }
        }

        private PcmDataLib.PcmData.ValueRepresentationType SampleFormatToVRT(WasapiCS.SampleFormatType t) {
            switch (t) {
            case WasapiCS.SampleFormatType.Sfloat:
                return PcmDataLib.PcmData.ValueRepresentationType.SFloat;
            case WasapiCS.SampleFormatType.Sint16:
            case WasapiCS.SampleFormatType.Sint24:
            case WasapiCS.SampleFormatType.Sint32V24:
            case WasapiCS.SampleFormatType.Sint32:
                return PcmDataLib.PcmData.ValueRepresentationType.SInt;
            default:
                System.Diagnostics.Debug.Assert(false);
                return PcmDataLib.PcmData.ValueRepresentationType.SInt;
            }
        }

        private void SaveRecordedData() {
            var pcm = mWasapiCtrl.GetCapturedData();
            var nFrames = pcm.Length / WasapiCS.SampleFormatTypeToUseBitsPerSample(mPref.SampleFormat) / mPref.NumOfChannels * 8;
            if (pcm == null || nFrames == 0) {
                return;
            }

            textBoxLog.Text += string.Format("captured frames={0} ({1:F1} {2}) glichCount={3}\r\n",
                nFrames, (double)nFrames / mPref.SampleRate, 
                Properties.Resources.Seconds, mWasapiCtrl.GetCaptureGlitchCount());

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.Filter = "WAVE files|*.wav";

            Nullable<bool> result = dlg.ShowDialog();

            if (result != true) {
                return;
            }

            var ww = new WavRWLib2.WavWriter();
            try {
                using (BinaryWriter bw = new BinaryWriter(File.Open(dlg.FileName, FileMode.Create))) {
                    ww.Write(bw, mPref.NumOfChannels,
                        WasapiCS.SampleFormatTypeToUseBitsPerSample(mPref.SampleFormat),
                        WasapiCS.SampleFormatTypeToValidBitsPerSample(mPref.SampleFormat),
                        mPref.SampleRate, SampleFormatToVRT(mPref.SampleFormat), nFrames, pcm);

                    textBoxLog.Text += string.Format("{0} : {1}\r\n", Properties.Resources.SaveFileSucceeded, dlg.FileName);
                }
            } catch (Exception ex) {
                string s = string.Format("{0} : {1}\r\n{2}\r\n", Properties.Resources.SaveFileFailed, dlg.FileName, ex);
                MessageBox.Show(s);
                AddLog(s);
            }

            slider1.Value = 0;
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        private void radioButton44100_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.SampleRate = 44100;
        }

        private void radioButton48000_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.SampleRate = 48000;
        }

        private void radioButton88200_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.SampleRate = 88200;
        }

        private void radioButton96000_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.SampleRate = 96000;
        }

        private void radioButton176400_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.SampleRate = 176400;
        }

        private void radioButton192000_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.SampleRate = 192000;
        }

        private void radioButton16_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.SampleFormat = WasapiCS.SampleFormatType.Sint16;
        }

        private void radioButton24_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.SampleFormat = WasapiCS.SampleFormatType.Sint24;
        }

        private void radioButton32v24_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.SampleFormat = WasapiCS.SampleFormatType.Sint32V24;
        }

        private void radioButton32_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.SampleFormat = WasapiCS.SampleFormatType.Sint32;
        }

        private void radioButtonEventDriven_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.WasapiDataFeedMode = WasapiCS.DataFeedMode.EventDriven;
        }

        private void radioButtonTimerDriven_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.WasapiDataFeedMode = WasapiCS.DataFeedMode.TimerDriven;
        }

        private void comboBoxLang_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            string cultureName = mResourceCultureNameArray[comboBoxLang.SelectedIndex];
            var newCulture = new CultureInfo(cultureName);

            Thread.CurrentThread.CurrentCulture = newCulture;
            Thread.CurrentThread.CurrentUICulture = newCulture;

            UpdateUITexts();
        }

        private static int GetBytesPerSec(Preference pref) {
            return pref.NumOfChannels * pref.SampleRate *
                WasapiCS.SampleFormatTypeToUseBitsPerSample(pref.SampleFormat) / 8;
        }

        private void textBoxRecordingBufferSizeMB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            if (!mInitialized) {
                return;
            } 
            
            int sizeMB = 0;
            if (!Int32.TryParse(textBoxRecordingBufferSizeMB.Text, out sizeMB)) {
                return;
            }
            
            int currentSec = 0;
            int maxSec = (int)((long)sizeMB * 1024 * 1024 / GetBytesPerSec(mPref));
            UpdateDurationLabel(currentSec, maxSec);

            if (sizeMB <= 0 || 2048 < sizeMB) {
                MessageBox.Show(Properties.Resources.ErrorRecordingBufferSize,
                    Properties.Resources.ErrorRecordingBufferSize, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void radioButtonPeakHold1sec_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mPref.PeakHoldSeconds = 1;
            mLevelMeter = new LevelMeter(mPref.SampleFormat, mPref.NumOfChannels,
                mPref.PeakHoldSeconds, mPref.WasapiBufferSizeMS * 0.001, mPref.ReleaseTimeDbPerSec);
        }

        private void radioButtonPeakHold3sec_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.PeakHoldSeconds = 3;
            mLevelMeter = new LevelMeter(mPref.SampleFormat, mPref.NumOfChannels,
                mPref.PeakHoldSeconds, mPref.WasapiBufferSizeMS * 0.001, mPref.ReleaseTimeDbPerSec);
        }

        private void radioButtonPeakHoldInfinity_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.PeakHoldSeconds = -1;
            mLevelMeter = new LevelMeter(mPref.SampleFormat, mPref.NumOfChannels,
                mPref.PeakHoldSeconds, mPref.WasapiBufferSizeMS * 0.001, mPref.ReleaseTimeDbPerSec);
        }

        private void radioButtonNominalPeakM6_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.YellowLevelDb = -6;
            UpdateLevelMeterScale();
        }

        private void radioButtonNominalPeakM10_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.YellowLevelDb = -10;
            UpdateLevelMeterScale();
        }

        private void radioButtonNominalPeakM12_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.YellowLevelDb = -12;
            UpdateLevelMeterScale();
        }

        private void checkBoxLevelMeterUpdateWhileRecording_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mPref.UpdateLevelMeterWhileRecording = true;

            mWasapiCtrl.SetCaptureCallback(ControlCaptureCallback);
        }

        private void checkBoxLevelMeterUpdateWhileRecording_Unchecked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mPref.UpdateLevelMeterWhileRecording = false;

            if (!buttonRec.IsEnabled) {
                // 録音中。
                mWasapiCtrl.SetCaptureCallback(null);
                ResetLevelMeter();
            }
        }

        private void buttonPeakHoldReset_Click(object sender, RoutedEventArgs e) {
            mLevelMeter.PeakHoldReset();
        }

        private void checkBoxSetDwChannelMask_Checked(object sender, RoutedEventArgs e) {
            mPref.SetDwChannelMask = true;
        }

        private void checkBoxSetDwChannelMask_Unchecked(object sender, RoutedEventArgs e) {
            mPref.SetDwChannelMask = false;
        }

        private void textBoxLevelMeterReleaseTime_TextChanged(object sender, TextChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            int v;
            if (Int32.TryParse(textBoxLevelMeterReleaseTime.Text, out v) && 0 <= v) {
                mPref.ReleaseTimeDbPerSec = v;
                mLevelMeter = new LevelMeter(mPref.SampleFormat, mPref.NumOfChannels, mPref.PeakHoldSeconds,
                    mPref.WasapiBufferSizeMS * 0.001, mPref.ReleaseTimeDbPerSec);
            } else {
                MessageBox.Show(Properties.Resources.ErrorReleaseTimeMustBePositiveInteger);
            }
        }
    }
}

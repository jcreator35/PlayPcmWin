using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Wasapi;

namespace RecPcmWin {
    public partial class MainWindow : Window {
        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        private const int MAX_RECORDING_BUFFER_MB = 2097151;

        private WasapiControl mWasapiCtrl = new WasapiControl();
        private Preference mPref = null;
        private BackgroundWorker mBW;
        private bool mInitialized = false;
        private List<WasapiCS.DeviceAttributes> mDeviceList = null;
        private LevelMeter mLevelMeter;
        private object mLock = new Object();

        private readonly int[] gComboBoxItemSampleRate = new int[] {
            44100,
            48000,
            64000,
            88200,
            96000,

            128000,
            176400,
            192000,
            352800,
            384000,

            705600,
            768000,
            1411200,
            1536000,
            2822400,
            3072000,
        };

        private string[] mResourceCultureNameArray = new string[] {
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
            groupBoxLevelMeter.Header = Properties.Resources.MainLevelMeter;
            radioButtonEventDriven.Content = Properties.Resources.EventDriven;
            radioButtonTimerDriven.Content = Properties.Resources.TimerDriven;
            buttonInspectDevice.Content = Properties.Resources.MainAvailableFormats;
            buttonRec.Content = Properties.Resources.MainRecord;
            buttonStop.Content = Properties.Resources.MainStop;
            buttonSelectDevice.Content = Properties.Resources.MainSelect;
            buttonDeselectDevice.Content = Properties.Resources.MainDeselect;
            labelLanguage.Content = Properties.Resources.MainLanguage;
            checkBoxSetDwChannelMask.Content = Properties.Resources.MainCheckboxSetDwChannelMask;
            groupBoxDwChannelMask.Header = Properties.Resources.MainGroupBoxDwChannelMask;
            groupBoxMasterVolumeControl.Header = Properties.Resources.MainGroupBoxMasterVolumeControl;
            checkBoxLevelMeterUpdateWhileRecording.Content = Properties.Resources.MainLevelMeterUpdateWhileRecording;
            mLevelMeterUC.UpdateUITexts();
        }

        private void PreferenceToUI() {
            for (int i = 0; i < gComboBoxItemSampleRate.Length; ++i) {
                if (mPref.SampleRate == gComboBoxItemSampleRate[i]) {
                    comboBoxSampleRate.SelectedIndex = i;
                }
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

            checkBoxSetDwChannelMask.IsChecked = mPref.SetDwChannelMask;
            checkBoxLevelMeterUpdateWhileRecording.IsChecked = mPref.UpdateLevelMeterWhileRecording;

            // Level Meter params
            mLevelMeterUC.PreferenceToUI(mPref.PeakHoldSeconds, mPref.YellowLevelDb, mPref.ReleaseTimeDbPerSec);
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
                mLevelMeterUC.ResetLevelMeter();
            }
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
                if (megaBytes <= 0 || 2097151 < megaBytes) {
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
                dwChannelMask = WasapiCS.GetTypicalChannelMask(mPref.NumOfChannels);
            }

            if (!mWasapiCtrl.AllocateCaptureMemory(
                    1024L * 1024 * mPref.RecordingBufferSizeMB)) {
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
                    string s = string.Format("Error: wasapi.Setup({0}Hz, {1}, {2}ms, {3}, {4}ch, dwChannelMask={5})\r\nError code = {6:X8} {7}\r\n",
                            mPref.SampleRate, mPref.SampleFormat,
                            mPref.WasapiBufferSizeMS, mPref.WasapiDataFeedMode,
                            mPref.NumOfChannels, dwChannelMask, hr, mWasapiCtrl.ErrorCodeToStr(hr));
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
                dwChannelMask = WasapiCS.GetTypicalChannelMask(mPref.NumOfChannels);
            }

            {
                string s = mWasapiCtrl.InspectDevice(listBoxDevices.SelectedIndex, dwChannelMask);
                AddLog(s);
            }
        }

        private long TotalFrames() {
            if (mPref.RecordingBufferSizeMB < 0) {
                return 0;
            }

            return (long)mPref.RecordingBufferSizeMB * 1024 * 1024 / WasapiCS.SampleFormatTypeToUseBitsPerSample(mPref.SampleFormat) / mPref.NumOfChannels * 8;
        }

        private void buttonSelectDevice_Click(object sender, RoutedEventArgs e) {
            if (DeviceSetup() < 0) {
                return;
            }

            mLevelMeter = new LevelMeter(mPref.SampleFormat, mPref.NumOfChannels, mPref.PeakHoldSeconds,
                mPref.WasapiBufferSizeMS * 0.001, mPref.ReleaseTimeDbPerSec);
            mWasapiCtrl.SetCaptureCallback(ControlCaptureCallback);
            mWasapiCtrl.StorePcm(false);

            int hr = mWasapiCtrl.StartRecording();
            if (hr < 0) {
                MessageBox.Show(string.Format("Select device failed! 0x{0:X8}", hr));
                return;
            }

            buttonSelectDevice.IsEnabled = false;
            buttonDeselectDevice.IsEnabled = true;
            buttonRec.IsEnabled = true;
            buttonStop.IsEnabled = false;
            buttonInspectDevice.IsEnabled = false;
            groupBoxWasapiSettings.IsEnabled = false;

            var volumeParams = mWasapiCtrl.GetVolumeParams();
            sliderMasterVolume.Minimum = volumeParams.levelMinDB;
            sliderMasterVolume.Maximum = volumeParams.levelMaxDB;
            var tickMarks = new DoubleCollection();
            for (int i = 0; i < (volumeParams.levelMaxDB - volumeParams.levelMinDB) / volumeParams.volumeIncrementDB; ++i) {
                tickMarks.Add(volumeParams.levelMinDB + (double)i * volumeParams.volumeIncrementDB);
            }
            sliderMasterVolume.Ticks = tickMarks;
            sliderMasterVolume.IsSnapToTickEnabled = true;
            sliderMasterVolume.Value = volumeParams.defaultLevel;
            sliderMasterVolume.IsEnabled = true;
            labelRecordingVolume.Content = string.Format("{0} dB", sliderMasterVolume.Value);
            mWasapiCtrl.SetEndpointMasterVolume((float)sliderMasterVolume.Value);

            if ((volumeParams.hardwareSupport & 1) == 1) {
                AddLog("This device supports hardware volume control.\r\n");
            } else {
                AddLog("This device does not support hardware volume control.\r\n");
            }

            mLevelMeterUC.UpdateNumOfChannels(mPref.NumOfChannels);

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
            sliderMasterVolume.IsEnabled = false;
            AddLog(string.Format("wasapi.Stop()\r\n"));
        }

        private void buttonRec_Click(object sender, RoutedEventArgs e) {
            if (checkBoxLevelMeterUpdateWhileRecording.IsChecked != true) {
                mWasapiCtrl.SetCaptureCallback(null);
                mLevelMeterUC.ResetLevelMeter();
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
            sliderMasterVolume.IsEnabled = false;
            AddLog(string.Format("wasapi.Stop()\r\n"));
        }

        private void ProgressChanged(object o, ProgressChangedEventArgs args) {
            if (!mWasapiCtrl.IsRunning()) {
                return;
            }

            slider1.Value = mWasapiCtrl.GetPosFrame();

            double currentSec = (double)mWasapiCtrl.GetPosFrame() / mPref.SampleRate;
            
            long nFrames = mWasapiCtrl.GetNumFrames();
            double maxSec = (double)nFrames / mPref.SampleRate;
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
            mLevelMeterUC.ResetLevelMeter();
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
            long nFrames = pcm.LongLength / WasapiCS.SampleFormatTypeToUseBitsPerSample(mPref.SampleFormat) / mPref.NumOfChannels * 8;
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

            try {
                using (BinaryWriter bw = new BinaryWriter(File.Open(dlg.FileName, FileMode.Create, FileAccess.Write, FileShare.Write))) {
                    WavRWLib2.WavWriter.Write(bw, mPref.NumOfChannels,
                            WasapiCS.SampleFormatTypeToUseBitsPerSample(mPref.SampleFormat),
                            mPref.SampleRate, nFrames, pcm);
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

            if (sizeMB <= 0 || MAX_RECORDING_BUFFER_MB < sizeMB) {
                MessageBox.Show(Properties.Resources.ErrorRecordingBufferSize,
                    Properties.Resources.ErrorRecordingBufferSize, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void checkBoxSetDwChannelMask_Checked(object sender, RoutedEventArgs e) {
            mPref.SetDwChannelMask = true;
        }

        private void checkBoxSetDwChannelMask_Unchecked(object sender, RoutedEventArgs e) {
            mPref.SetDwChannelMask = false;
        }

        private void sliderMasterVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!mInitialized) {
                return;
            }

            labelRecordingVolume.Content = string.Format("{0} dB", sliderMasterVolume.Value);
            mWasapiCtrl.SetEndpointMasterVolume((float)sliderMasterVolume.Value);
        }

        private void comboBoxSampleRate_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPref.SampleRate = gComboBoxItemSampleRate[comboBoxSampleRate.SelectedIndex];
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
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

            mLevelMeterUC.SetParamChangedCallback(LevelMeterUCParamChanged);

            mInitialized = true;
        }

        /// <summary>
        /// LevelMeterユーザーコントロールの設定がユーザー操作によって変更されたとき呼び出される。
        /// </summary>
        private void LevelMeterUCParamChanged(
                int peakHoldSeconds, int yellowLevelDb, int releaseTimeDbPerSec, bool meterReset) {
            mPref.PeakHoldSeconds = peakHoldSeconds;
            mPref.YellowLevelDb = yellowLevelDb;
            mPref.ReleaseTimeDbPerSec = releaseTimeDbPerSec;

            lock (mLock) {
                mLevelMeter = new LevelMeter(mPref.SampleFormat, mPref.NumOfChannels, mPref.PeakHoldSeconds,
                    mPref.WasapiBufferSizeMS * 0.001, mPref.ReleaseTimeDbPerSec);
            }
        }

        private void ControlCaptureCallback(byte[] pcmData) {
            // このスレッドは描画できないので注意。

            double[] peakDb;
            double[] peakHoldDb;

            lock (mLock) {
                mLevelMeter.Update(pcmData);

                if (mLevelMeter.NumChannels <= 2) {
                    peakDb = new double[2];
                    peakHoldDb = new double[2];

                    for (int ch = 0; ch < 2; ++ch) {
                        peakDb[ch] = mLevelMeter.GetPeakDb(ch);
                        peakHoldDb[ch] = mLevelMeter.GetPeakHoldDb(ch);
                    }
                } else {
                    peakDb = new double[8];
                    peakHoldDb = new double[8];

                    for (int ch = 0; ch < 8; ++ch) {
                        peakDb[ch] = mLevelMeter.GetPeakDb(ch);
                        peakHoldDb[ch] = mLevelMeter.GetPeakHoldDb(ch);
                    }
                }
            }

            Dispatcher.BeginInvoke(new Action(delegate() {
                // 描画スレッドで描画する。
                mLevelMeterUC.UpdateLevelMeter(peakDb, peakHoldDb);
            }));
        }
    }
}

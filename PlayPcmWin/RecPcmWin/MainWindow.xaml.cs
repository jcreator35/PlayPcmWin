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

namespace RecPcmWin {
    public partial class MainWindow : Window {
        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }
        
        private WasapiControl mWasapiCtrl = new WasapiControl();
        private Preference mPref = null;
        private BackgroundWorker mBW;
        private bool mInitialized = false;
        private List<WasapiCS.DeviceAttributes> mDeviceList = null;

        public void AddLog(string text) {
            textBoxLog.Text += text;
            textBoxLog.ScrollToEnd();
        }

        public MainWindow() {
            InitializeComponent();

            mPref = PreferenceStore.Load();

            int hr = 0;
            hr = mWasapiCtrl.Init();
            AddLog(string.Format("RecPcmWin version {0}\r\n", AssemblyVersion));
            AddLog(string.Format("wasapi.Init() {0:X8}\r\n", hr));

            Closed += new EventHandler(MainWindow_Closed);

            CreateDeviceList();

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

            PreferenceToUI();

            mInitialized = true;
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

            buttonRec.IsEnabled              = mDeviceList.Count != 0;
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
                mPref.SampleRate, mPref.SampleFormat, mPref.NumOfChannels);
            {
                if (hr < 0) {
                    string s = string.Format("Error: wasapi.Setup({0}Hz, {1}, {2}ms, {3}, {4}ch)\r\nError code = {5:X8}\r\n",
                            mPref.SampleRate, mPref.SampleFormat,
                            mPref.WasapiBufferSizeMS, mPref.WasapiDataFeedMode,
                            mPref.NumOfChannels, hr);
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

            buttonRec.IsEnabled = true;
            buttonInspectDevice.IsEnabled = false;
            groupBoxWasapiSettings.IsEnabled = false;

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

            {
                string s = mWasapiCtrl.InspectDevice(listBoxDevices.SelectedIndex, mPref.NumOfChannels);
                AddLog(s);
            }
        }

        private int TotalFrames() {
            if (mPref.RecordingBufferSizeMB < 0) {
                return 0;
            }

            return mPref.RecordingBufferSizeMB * 1024 * 1024 / WasapiCS.SampleFormatTypeToUseBitsPerSample(mPref.SampleFormat) / mPref.NumOfChannels * 8;
        }

        private void buttonRec_Click(object sender, RoutedEventArgs e) {
            if (DeviceSetup() < 0) {
                return;
            }

            AddLog("wasapi.StartRecording()\r\n");
            mWasapiCtrl.StartRecording();

            slider1.Value = 0;
            slider1.Maximum = TotalFrames();
            buttonStop.IsEnabled     = true;
            buttonRec.IsEnabled      = false;

            mBW = new BackgroundWorker();
            mBW.WorkerReportsProgress = true;
            mBW.DoWork += new DoWorkEventHandler(DoWork);
            mBW.ProgressChanged += new ProgressChangedEventHandler(ProgressChanged);
            mBW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(RunWorkerCompleted);
            mBW.RunWorkerAsync();
        }

        private void ProgressChanged(object o, ProgressChangedEventArgs args) {
            if (!mWasapiCtrl.IsRunning()) {
                return;
            }

            slider1.Value = mWasapiCtrl.GetPosFrame();

            double currentSec = (double)mWasapiCtrl.GetPosFrame() / mPref.SampleRate;
            double maxSec = (double)mWasapiCtrl.GetNumFrames() / mPref.SampleRate;

            label1.Content = string.Format(CultureInfo.CurrentCulture, "{0:F1} / {1:F1}",
                currentSec, maxSec);
        }

        private void RunWorkerCompleted(object o, RunWorkerCompletedEventArgs args) {
            AddLog(string.Format("Recording completed.\r\n"));

            SaveRecordedData();

            AddLog("wasapi.Unsetup()\r\n");
            mWasapiCtrl.Unsetup();
            mWasapiCtrl.ReleaseCaptureMemory();

            RecordStopped();
        }

        private void RecordStopped() {
            buttonInspectDevice.IsEnabled = true;
            buttonRec.IsEnabled = true;
            buttonStop.IsEnabled = false;
            groupBoxWasapiSettings.IsEnabled = true;
        }

        private void DoWork(object o, DoWorkEventArgs args) {
            while (!mWasapiCtrl.Run(200)) {
                mBW.ReportProgress(0);
                System.Threading.Thread.Sleep(1);
            }

            mWasapiCtrl.Stop();
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e) {
            buttonStop.IsEnabled = false;

            mWasapiCtrl.Stop();
            AddLog(string.Format("wasapi.Stop()\r\n"));
            buttonStop.IsEnabled = false;
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
    }
}

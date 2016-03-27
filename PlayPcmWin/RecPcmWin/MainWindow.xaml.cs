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
        private PcmData mPcmData = null;
        private int mBufferBytes = 0;
        private int mSamplingFrequency = 44100;
        private int mNumChannels = 2;
        private WasapiCS.SampleFormatType mSampleFormat = WasapiCS.SampleFormatType.Sint16;
        private BackgroundWorker mBW;

        public void AddLog(string text) {
            textBoxLog.Text += text;
            textBoxLog.ScrollToEnd();
        }

        public MainWindow() {
            InitializeComponent();

            int hr = 0;
            hr = mWasapiCtrl.Init();
            AddLog(string.Format("RecPcmWin version {0}\r\n", AssemblyVersion));
            AddLog(string.Format("wasapi.Init() {0:X8}\r\n", hr));

            Closed += new EventHandler(MainWindow_Closed);

            CreateDeviceList();
        }

        private void CreateDeviceList() {
            int hr;

            int selectedIndex = -1;
            if (0 < listBoxDevices.Items.Count) {
                selectedIndex = listBoxDevices.SelectedIndex;
            }

            listBoxDevices.Items.Clear();

            var deviceNameList = new List<string>();
            hr = mWasapiCtrl.EnumerateRecDeviceNames(deviceNameList);
            textBoxLog.Text += string.Format("wasapi.DoDeviceEnumeration(Rec) {0:X8}\r\n", hr);

            foreach (var d in deviceNameList) {
                listBoxDevices.Items.Add(d);
            }

            buttonRec.IsEnabled              = deviceNameList.Count != 0;
            buttonStop.IsEnabled             = false;
            groupBoxWasapiSettings.IsEnabled = true;
            buttonInspectDevice.IsEnabled = deviceNameList.Count != 0;

            if (deviceNameList.Count == 0) {
                return;
            }

            // 選択されていた項目を選択状態にする。
            if (0 <= selectedIndex && selectedIndex < listBoxDevices.Items.Count) {
                listBoxDevices.SelectedIndex = selectedIndex;
            } else {
                listBoxDevices.SelectedIndex = 0;
            }
        }

        void MainWindow_Closed(object sender, EventArgs e) {
            mWasapiCtrl.Term();

            Application.Current.Shutdown(0);
        }

        private int DeviceSetup() {
            int hr;

            // read latency millisec from the textbox
            int latencyMillisec = -1;
            bool result = Int32.TryParse(textBoxLatency.Text, out latencyMillisec);
            if (!result || latencyMillisec <= 0) {
                string s = Properties.Resources.ErrorWasapiBufferSize;
                MessageBox.Show(s);
                AddLog(s);
                return -1;
            }

            // read num of channels
            result = Int32.TryParse(textBoxNumOfChannels.Text, out mNumChannels);
            if (!result || mNumChannels <= 0) {
                string s = Properties.Resources.ErrorNumChannels;
                MessageBox.Show(s);
                AddLog(s);
                return -1;
            }

            // read recording buffer size
            int megaBytes = 0;
            result = Int32.TryParse(textBoxRecMaxMB.Text, out megaBytes);
            if (megaBytes <= 0 || 2047 < megaBytes) {
                string s = Properties.Resources.ErrorRecordingBufferSize;
                MessageBox.Show(s);
                AddLog(s);
                return -1;
            }
            mBufferBytes = megaBytes * 1024 * 1024;

            if (!mWasapiCtrl.AllocateCaptureMemory(mBufferBytes)) {
                string s = string.Format("{0}\r\n", Properties.Resources.ErrorCouldNotAllocateMemory);
                MessageBox.Show(s);
                AddLog(s);
                return -1;
            }

            WasapiCS.DataFeedMode dfm = WasapiCS.DataFeedMode.EventDriven;
            if (true == radioButtonTimerDriven.IsChecked) {
                dfm = WasapiCS.DataFeedMode.TimerDriven;
            }

            hr = mWasapiCtrl.Setup(listBoxDevices.SelectedIndex, dfm, latencyMillisec, mSamplingFrequency, mSampleFormat, mNumChannels);
            {
                if (hr < 0) {
                    string s = string.Format("Error: wasapi.Setup({0}Hz, {1}, {2}ms, {3}, {4}ch)\r\nError code = {5:X8}\r\n",
                            mSamplingFrequency, mSampleFormat, latencyMillisec, dfm, mNumChannels, hr);
                    MessageBox.Show(s);
                    AddLog(s);

                    AddLog("wasapi.Unsetup()\r\n");
                    mWasapiCtrl.Unsetup();
                    mWasapiCtrl.ReleaseCaptureMemory();
                    return hr;
                } else {
                    string s = string.Format("wasapi.Setup({0}Hz, {1}, {2}ms, {3}, {4}ch) Succeeded {5}\r\n",
                            mSamplingFrequency, mSampleFormat, latencyMillisec, dfm, mNumChannels, hr);
                    AddLog(s);
                }
            }

            buttonRec.IsEnabled = true;
            buttonInspectDevice.IsEnabled = false;
            groupBoxWasapiSettings.IsEnabled = false;
            return 0;
        }

        private void buttonInspectDevice_Click(object sender, RoutedEventArgs e) {
            string s = mWasapiCtrl.InspectDevice(listBoxDevices.SelectedIndex, mNumChannels);
            AddLog(s);
        }

        private int TotalFrames() {
            if (mBufferBytes < 0) {
                return 0;
            }

            return mBufferBytes / WasapiCS.SampleFormatTypeToUseBitsPerSample(mSampleFormat) / mNumChannels * 8;
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

            double currentSec = (double)mWasapiCtrl.GetPosFrame() / mSamplingFrequency;
            double maxSec = (double)mWasapiCtrl.GetNumFrames() / mSamplingFrequency;

            int currentMin = (int)(currentSec / 60);
            currentSec -= currentMin * 60;
            int maxMin = (int)(maxSec/60);
            maxSec -= maxMin * 60;

            label1.Content = string.Format("{0}:{1:F1} / {2}:{3:F1} sec",
                currentMin, currentSec,
                maxMin, maxSec);
        }

        private void RunWorkerCompleted(object o, RunWorkerCompletedEventArgs args) {
            AddLog(string.Format("Rec completed.\r\n"));

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
            var nFrames = pcm.Length / WasapiCS.SampleFormatTypeToUseBitsPerSample(mSampleFormat) / mNumChannels * 8;
            if (pcm == null || nFrames == 0) {
                return;
            }

            textBoxLog.Text += string.Format("captured frames={0} ({1:F1} seconds) glichCount={2}\r\n",
                nFrames, (double)nFrames / mSamplingFrequency, mWasapiCtrl.GetCaptureGlitchCount());

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
                    ww.Write(bw, mNumChannels, WasapiCS.SampleFormatTypeToUseBitsPerSample(mSampleFormat), WasapiCS.SampleFormatTypeToValidBitsPerSample(mSampleFormat),
                        mSamplingFrequency, SampleFormatToVRT(mSampleFormat), nFrames, pcm);

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
            mSamplingFrequency = 44100;
        }

        private void radioButton48000_Checked(object sender, RoutedEventArgs e) {
            mSamplingFrequency = 48000;
        }

        private void radioButton88200_Checked(object sender, RoutedEventArgs e) {
            mSamplingFrequency = 88200;
        }

        private void radioButton96000_Checked(object sender, RoutedEventArgs e) {
            mSamplingFrequency = 96000;
        }

        private void radioButton176400_Checked(object sender, RoutedEventArgs e) {
            mSamplingFrequency = 176400;
        }

        private void radioButton192000_Checked(object sender, RoutedEventArgs e) {
            mSamplingFrequency = 192000;
        }

        private void radioButton16_Checked(object sender, RoutedEventArgs e) {
            mSampleFormat = WasapiCS.SampleFormatType.Sint16;
        }

        private void radioButton24_Checked(object sender, RoutedEventArgs e) {
            mSampleFormat = WasapiCS.SampleFormatType.Sint24;
        }

        private void radioButton32v24_Checked(object sender, RoutedEventArgs e) {
            mSampleFormat = WasapiCS.SampleFormatType.Sint32V24;
        }

        private void radioButton32_Checked(object sender, RoutedEventArgs e) {
            mSampleFormat = WasapiCS.SampleFormatType.Sint32;
        }
    }
}

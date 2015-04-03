using System;
using System.Text;
using System.Windows;
using Wasapi;
using WavRWLib2;
using System.IO;
using System.ComponentModel;
using System.Globalization;

namespace RecPcmWin {
    public partial class MainWindow : Window {
        private WasapiCS wasapi;

        WavData mWavData = null;

        const int DEFAULT_BUFFER_SIZE_MB    = 256;
        const int DEFAULT_OUTPUT_LATENCY_MS = 200;
        int mSamplingFrequency     = 44100;
        int mNumChannels = 2;
        WasapiCS.SampleFormatType mSampleFormat = WasapiCS.SampleFormatType.Sint16;
        byte[] mCapturedPcmData;

        public MainWindow() {
            InitializeComponent();

            int hr = 0;
            wasapi = new WasapiCS();
            hr = wasapi.Init();
            textBoxLog.Text += string.Format("wasapi.Init() {0:X8}\r\n", hr);
            textBoxLatency.Text = string.Format("{0}", DEFAULT_OUTPUT_LATENCY_MS);
            textBoxRecMaxMB.Text = string.Format("{0}", DEFAULT_BUFFER_SIZE_MB);

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

            hr = wasapi.DoDeviceEnumeration(WasapiCS.DeviceType.Rec);
            textBoxLog.Text += string.Format("wasapi.DoDeviceEnumeration(Rec) {0:X8}\r\n", hr);

            int nDevices = wasapi.GetDeviceCount();
            for (int i = 0; i < nDevices; ++i) {
                listBoxDevices.Items.Add(wasapi.GetDeviceName(i));
            }

            buttonDeviceSelect.IsEnabled     = true;
            buttonDeselect.IsEnabled         = false;
            buttonRec.IsEnabled              = false;
            buttonStop.IsEnabled             = false;
            groupBoxWasapiSettings.IsEnabled = true;
            buttonInspectDevice.IsEnabled    = false;

            if (0 < nDevices) {
                if (0 <= selectedIndex && selectedIndex < listBoxDevices.Items.Count) {
                    listBoxDevices.SelectedIndex = selectedIndex;
                } else {
                    listBoxDevices.SelectedIndex = 0;
                }

                if (mWavData != null) {
                    buttonDeviceSelect.IsEnabled = true;
                }
                buttonInspectDevice.IsEnabled = true;
            }
        }

        void MainWindow_Closed(object sender, EventArgs e) {
            wasapi.Stop();
            wasapi.Unsetup();
            wasapi.Term();
            wasapi = null;

            Application.Current.Shutdown(0);
        }

        private static string DfmToStr(WasapiCS.DataFeedMode dfm) {
            switch (dfm) {
            case WasapiCS.DataFeedMode.EventDriven:
                return "イベント駆動モード";
            case WasapiCS.DataFeedMode.TimerDriven:
                return "タイマー駆動モード";
            default:
                System.Diagnostics.Debug.Assert(false);
                return "unknown";
            }
        }

        private void buttonDeviceSelect_Click(object sender, RoutedEventArgs e) {
            int latencyMillisec = -1;
            try {
                latencyMillisec = Int32.Parse(textBoxLatency.Text);
            } catch (Exception ex) {
                textBoxLog.Text += string.Format("{0}\r\n", ex);
            }
            if (latencyMillisec <= 0) {
                latencyMillisec = DEFAULT_OUTPUT_LATENCY_MS;
                textBoxLatency.Text = string.Format("{0}", DEFAULT_OUTPUT_LATENCY_MS);
            }

            try {
                var bytes = Int32.Parse(textBoxRecMaxMB.Text) * 1024L * 1024L;
                if (0x7fffffff < bytes) {
                    string s = string.Format("E: 録音バッファーサイズを2047MB以下に減らして下さい。\r\n");
                    textBoxLog.Text += s;
                    MessageBox.Show(s);
                    return;
                }
                m_bufferBytes = (int)bytes;
            } catch (Exception ex) {
                textBoxLog.Text += string.Format("{0}\r\n", ex);
            }
            if (m_bufferBytes < 0) {
                m_bufferBytes = DEFAULT_BUFFER_SIZE_MB * 1024 * 1024;
                textBoxRecMaxMB.Text = string.Format("{0}", DEFAULT_BUFFER_SIZE_MB);
            }
            try {
                mCapturedPcmData = null;
                mCapturedPcmData = new byte[m_bufferBytes];
            } catch (Exception ex) {
                textBoxLog.Text += string.Format("{0}\r\n", ex);
                MessageBox.Show(string.Format("E: おそらくメモリ不足ですので、録音バッファーサイズを減らして下さい。\r\n{0}", ex));
                return;
            }
            
            int hr = wasapi.ChooseDevice(listBoxDevices.SelectedIndex);
            textBoxLog.Text += string.Format("wasapi.ChooseDevice({0}) {1:X8}\r\n",
                listBoxDevices.SelectedItem.ToString(), hr);
            if (hr < 0) {
                return;
            }

            WasapiCS.DataFeedMode dfm = WasapiCS.DataFeedMode.EventDriven;
            if (true == radioButtonTimerDriven.IsChecked) {
                dfm = WasapiCS.DataFeedMode.TimerDriven;
            }
            
            wasapi.SetDataFeedMode(dfm);
            wasapi.SetLatencyMillisec(latencyMillisec);
            hr = wasapi.Setup(mSamplingFrequency, mSampleFormat, mNumChannels);
            textBoxLog.Text += string.Format("wasapi.Setup({0}, {1}, {2}, {3}) {4:X8}\r\n",
                mSamplingFrequency, mSampleFormat, latencyMillisec, dfm, hr);
            if (hr < 0) {
                wasapi.Unsetup();
                textBoxLog.Text += string.Format("wasapi.Unsetup()\r\n");
                CreateDeviceList();
                string sDfm = DfmToStr(dfm);
                string s = string.Format("E: wasapi.Setup({0}, {1}, {2}, {3})失敗。{4:X8}\nこのプログラムのバグか、オーディオデバイスが{0}Hz {1} レイテンシー{2}ms {3}に対応していないのか、どちらかです。\r\n",
                    mSamplingFrequency, mSampleFormat,
                    latencyMillisec, sDfm, hr);
                textBoxLog.Text += s;
                MessageBox.Show(s);
                return;
            }

            buttonDeviceSelect.IsEnabled     = false;
            buttonDeselect.IsEnabled         = true;
            buttonRec.IsEnabled              = true;
            buttonInspectDevice.IsEnabled    = false;
            groupBoxWasapiSettings.IsEnabled = false;
        }

        private void buttonDeviceDeselect_Click(object sender, RoutedEventArgs e) {
            textBoxLog.Text += string.Format("wasapi.Stop()\r\n");
            wasapi.Stop();
            textBoxLog.Text += string.Format("wasapi.Unsetup()\r\n");
            wasapi.Unsetup();
            CreateDeviceList();
        }

struct InspectFormat {
            public int sampleRate;
            public int bitsPerSample;
            public int validBitsPerSample;
            public int bitFormat; // 0:Int, 1:Float
            public InspectFormat(int sr, int bps, int vbps, int bf) {
                sampleRate = sr;
                bitsPerSample = bps;
                validBitsPerSample = vbps;
                bitFormat = bf;
            }
        };

        const int TEST_SAMPLE_RATE_NUM = 8;
        const int TEST_BIT_REPRESENTATION_NUM = 5;

        static readonly InspectFormat [] gInspectFormats = new InspectFormat [] {
                new InspectFormat(44100, 16, 16, 0),
                new InspectFormat(48000, 16, 16, 0),
                new InspectFormat(88200, 16, 16, 0),
                new InspectFormat(96000, 16, 16, 0),
                new InspectFormat(176400, 16, 16, 0),
                new InspectFormat(192000, 16, 16, 0),
                new InspectFormat(352800, 16, 16, 0),
                new InspectFormat(384000, 16, 16, 0),

                new InspectFormat(44100, 24, 24, 0),
                new InspectFormat(48000, 24, 24, 0),
                new InspectFormat(88200, 24, 24, 0),
                new InspectFormat(96000, 24, 24, 0),
                new InspectFormat(176400, 24, 24, 0),
                new InspectFormat(192000, 24, 24, 0),
                new InspectFormat(352800, 24, 24, 0),
                new InspectFormat(384000, 24, 24, 0),

                new InspectFormat(44100, 32, 24, 0),
                new InspectFormat(48000, 32, 24, 0),
                new InspectFormat(88200, 32, 24, 0),
                new InspectFormat(96000, 32, 24, 0),
                new InspectFormat(176400, 32, 24, 0),
                new InspectFormat(192000, 32, 24, 0),
                new InspectFormat(352800, 32, 24, 0),
                new InspectFormat(384000, 32, 24, 0),

                new InspectFormat(44100, 32, 32, 0),
                new InspectFormat(48000, 32, 32, 0),
                new InspectFormat(88200, 32, 32, 0),
                new InspectFormat(96000, 32, 32, 0),
                new InspectFormat(176400, 32, 32, 0),
                new InspectFormat(192000, 32, 32, 0),
                new InspectFormat(352800, 32, 32, 0),
                new InspectFormat(384000, 32, 32, 0),

                new InspectFormat(44100, 32, 32, 1),
                new InspectFormat(48000, 32, 32, 1),
                new InspectFormat(88200, 32, 32, 1),
                new InspectFormat(96000, 32, 32, 1),
                new InspectFormat(176400, 32, 32, 1),
                new InspectFormat(192000, 32, 32, 1),
                new InspectFormat(352800, 32, 32, 1),
                new InspectFormat(384000, 32, 32, 1),
            };

        private void buttonInspectDevice_Click(object sender, RoutedEventArgs e) {
            string dn = wasapi.GetDeviceName(listBoxDevices.SelectedIndex);
            string did = wasapi.GetDeviceIdString(listBoxDevices.SelectedIndex);

            textBoxLog.Text += string.Format(CultureInfo.InvariantCulture, "wasapi.InspectDevice()\r\nDeviceFriendlyName={0}\r\nDeviceIdString={1}\r\n", dn, did);
            textBoxLog.Text += "++-------------++-------------++-------------++-------------++-------------++-------------++-------------++-------------++\r\n";
            for (int fmt = 0; fmt < TEST_BIT_REPRESENTATION_NUM; ++fmt) {
                var sb = new StringBuilder();
                for (int sr =0; sr < TEST_SAMPLE_RATE_NUM; ++sr) {
                    int idx = sr + fmt * TEST_SAMPLE_RATE_NUM;
                    System.Diagnostics.Debug.Assert(idx < gInspectFormats.Length);
                    InspectFormat ifmt = gInspectFormats[idx];
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "||{0,3}kHz {1}{2}V{3}",
                        ifmt.sampleRate / 1000, ifmt.bitFormat == 0 ? "i" : "f",
                        ifmt.bitsPerSample, ifmt.validBitsPerSample));
                }
                sb.Append("||\r\n");
                textBoxLog.Text += sb.ToString();

                sb.Clear();
                for (int sr =0; sr < TEST_SAMPLE_RATE_NUM; ++sr) {
                    int idx = sr + fmt * TEST_SAMPLE_RATE_NUM;
                    System.Diagnostics.Debug.Assert(idx < gInspectFormats.Length);
                    InspectFormat ifmt = gInspectFormats[idx];
                    int hr = wasapi.InspectDevice(listBoxDevices.SelectedIndex, ifmt.sampleRate, ifmt.bitsPerSample, ifmt.validBitsPerSample, ifmt.bitFormat);
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "|| {0} {1:X8} ", hr==0 ? "OK" : "NA", hr));
                }
                sb.Append("||\r\n");
                textBoxLog.Text += sb.ToString();
                textBoxLog.Text += "++-------------++-------------++-------------++-------------++-------------++-------------++-------------++-------------++\r\n";
            }
        }

        BackgroundWorker bw;

        private int m_bufferBytes = -1;

        private void buttonRec_Click(object sender, RoutedEventArgs e) {
            wasapi.SetupCaptureBuffer(m_bufferBytes);
            textBoxLog.Text += string.Format("wasapi.SetupCaptureBuffer() {0:X8}\r\n", m_bufferBytes);

            int hr = wasapi.StartRecording();
            textBoxLog.Text += string.Format("wasapi.StartRecording() {0:X8}\r\n", hr);
            if (hr < 0) {
                return;
            }

            slider1.Value = 0;
            slider1.Maximum = wasapi.GetTotalFrameNum(WasapiCS.PcmDataUsageType.Capture);
            buttonStop.IsEnabled     = true;
            buttonRec.IsEnabled      = false;
            buttonDeselect.IsEnabled = false;

            bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork += new DoWorkEventHandler(DoWork);
            bw.ProgressChanged += new ProgressChangedEventHandler(ProgressChanged);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(RunWorkerCompleted);
            bw.RunWorkerAsync();
        }

        private void ProgressChanged(object o, ProgressChangedEventArgs args) {
            if (null == wasapi) {
                return;
            }
            slider1.Value = wasapi.GetPosFrame(WasapiCS.PcmDataUsageType.Capture);
            label1.Content = string.Format("{0:F1}/{1:F1}",
                slider1.Value / mSamplingFrequency,
                slider1.Maximum / mSamplingFrequency);
        }

        private void RunWorkerCompleted(object o, RunWorkerCompletedEventArgs args) {
            textBoxLog.Text += string.Format("Rec completed.\r\n");

            SaveRecordedData();

            buttonRec.IsEnabled = true;
            buttonStop.IsEnabled = false;
            buttonDeselect.IsEnabled = true;
        }

        private void DoWork(object o, DoWorkEventArgs args) {
            Console.WriteLine("DoWork started");

            while (!wasapi.Run(200)) {
                bw.ReportProgress(0);
                System.Threading.Thread.Sleep(1);
            }

            wasapi.Stop();

            Console.WriteLine("DoWork end");
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e) {
            buttonStop.IsEnabled = false;

            wasapi.Stop();
            textBoxLog.Text += string.Format("wasapi.Stop()\r\n");

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
            var bytes = wasapi.GetCapturedData(mCapturedPcmData);
            var nFrames = bytes / WasapiCS.SampleFormatTypeToUseBytesPerSample(mSampleFormat) / mNumChannels;

            if (nFrames == 0) {
                return;
            }

            textBoxLog.Text += string.Format("captured frames={0} ({1:F1} seconds) glichCount={2}\r\n",
                nFrames, (double)nFrames / mSamplingFrequency, wasapi.GetCaptureGlitchCount());

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".wav";
            dlg.Filter = "WAVEファイル|*.wav";

            Nullable<bool> result = dlg.ShowDialog();

            if (result != true) {
                return;
            }

            // あとで本のサイズに戻す。
            var originalSize = mCapturedPcmData.Length;
            Array.Resize(ref mCapturedPcmData, (int)bytes);

            mWavData = new WavData();
            mWavData.Set(mNumChannels, WasapiCS.SampleFormatTypeToUseBytesPerSample(mSampleFormat) * 8, WasapiCS.SampleFormatTypeToValidBitsPerSample(mSampleFormat),
                mSamplingFrequency, SampleFormatToVRT(mSampleFormat), nFrames, mCapturedPcmData);

            try {
                using (BinaryWriter w = new BinaryWriter(File.Open(dlg.FileName, FileMode.Create))) {
                    mWavData.Write(w);

                    textBoxLog.Text += string.Format("ファイル保存成功: {0}\r\n", dlg.FileName);
                }
            } catch (Exception ex) {
                string s = string.Format("E: ファイル保存失敗: {0}\r\n{1}\r\n", dlg.FileName, ex);
                textBoxLog.Text += s;
                MessageBox.Show(s);
            }

            slider1.Value = 0;
            label1.Content = "0/0";
            Array.Resize(ref mCapturedPcmData, originalSize);
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

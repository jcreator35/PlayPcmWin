using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Asio;
using WavRWLib2;

namespace AsioTestGUI
{
    public partial class Form1 : Form
    {
        private AsioCS asio;
        const int SAMPLE_RATE = 96000;

        public Form1()
        {
            InitializeComponent();

            asio = new AsioCS();
            asio.Init();
            int nDrivers = asio.DriverNumGet();

            Console.WriteLine("driverNum=" + nDrivers);
            for (int i = 0; i < nDrivers; ++i) {
                listBoxDrivers.Items.Add(asio.DriverNameGet(i));
            }
            if (0 < nDrivers) {
                listBoxDrivers.SelectedIndex = 0;
                buttonLoadDriver.Enabled = true;
            }

            if (1 == nDrivers) {
                buttonLoadDriver_Click(null, null);
            }
        }

        public void FinalizeAll()
        {
            asio.Term();
        }

        private void buttonLoadDriver_Click(object sender, EventArgs e)
        {
            buttonLoadDriver.Enabled = false;
            bool bRv = asio.DriverLoad(listBoxDrivers.SelectedIndex);
            if (!bRv) {
                return;
            }

            int rv = asio.Setup(SAMPLE_RATE);
            if (0 != rv) {
                string errStr = string.Empty;
                switch (rv) {
                case -5003: errStr = "Device Not Found"; break;
                case -1000: errStr = "hardware input or output is not present or available"; break;
                default: break;
                }
                if (errStr == string.Empty) {
                    MessageBox.Show(string.Format("ASIO setup({0}) failed {1:X8}", SAMPLE_RATE, rv));
                } else {
                    MessageBox.Show(string.Format("ASIO setup({0}) failed {1} ({2:X8})", SAMPLE_RATE, errStr, rv));
                }
                asio.Unsetup();
                asio.DriverUnload();
                buttonLoadDriver.Enabled = true;
                return;
            }

            for (int i = 0; i < asio.InputChannelsNumGet(); ++i) {
                listBoxInput.Items.Add(asio.InputChannelNameGet(i));
            }
            if (0 < listBoxInput.Items.Count) {
                listBoxInput.SelectedIndex = 0;
            }
            for (int i = 0; i < asio.OutputChannelsNumGet(); ++i) {
                listBoxOutput.Items.Add(asio.OutputChannelNameGet(i));
            }
            if (0 < listBoxOutput.Items.Count) {
                listBoxOutput.SelectedIndex = 0;
            }

            for (int i = 0; i < asio.ClockSourceNumGet(); ++i) {
                listBoxClockSources.Items.Add(asio.ClockSourceNameGet(i));
            }
            if (0 < listBoxOutput.Items.Count) {
                listBoxClockSources.SelectedIndex = 0;
            }

            if (0 == rv &&
                0 < listBoxInput.Items.Count &&
                0 < listBoxOutput.Items.Count) {
                buttonStart.Enabled = true;
            }
            listBoxDrivers.Enabled = false;
            buttonControlPanel.Enabled = true;
        }

        BackgroundWorker bw;
        int m_inputChannelNum;
        int m_seconds;
        string m_writeFilePath;

        private void DoWork(object o, DoWorkEventArgs args) {
            Console.WriteLine("DoWork started\n");

            int count = 0;
            while (!asio.Run()) {
                ++count;
                Console.WriteLine("\nForm1.DoWork() count={0} m_seconds={1}", count, m_seconds);
                int percent = 100 * count / m_seconds;
                if (100 < percent) {
                    percent = 100;
                }

                    
            }
            int[] recordedData = asio.RecordedDataGet(m_inputChannelNum, m_seconds * SAMPLE_RATE);
            PcmSamples1Channel ch0 = new PcmSamples1Channel(m_seconds * SAMPLE_RATE, 16);
            int max = 0;
            int min = 0;
            for (int i = 0; i < recordedData.Length; ++i) {
                if (max < recordedData[i]) {
                    max = recordedData[i];
                }
                if (recordedData[i] < min) {
                    min = recordedData[i];
                }
            }
            Console.WriteLine("max={0} min={1}", max, min);

            if (max < -min) {
                max = -min;
            }
            double mag = 32767.0 / max;
            Console.WriteLine("mag={0}", mag);

            for (int i = 0; i < recordedData.Length; ++i) {
                ch0.Set16(i, (short)(recordedData[i] * mag));
            }

            List<PcmSamples1Channel> chList = new List<PcmSamples1Channel>();
            chList.Add(ch0);

            WavData wd = new WavData();
            wd.Create(SAMPLE_RATE, 16, chList);
            using (BinaryWriter bw = new BinaryWriter(File.Open(m_writeFilePath, FileMode.Create))) {
                wd.Write(bw);
            }

            args.Result = 0;
            Console.WriteLine("DoWork end\n");
        }

        private void ProgressChanged(object o, ProgressChangedEventArgs args) {
            progressBar1.Value = args.ProgressPercentage;
        }

        private void RunWorkerCompleted(object o, RunWorkerCompletedEventArgs args) {
            progressBar1.Visible = false;
            buttonStart.Enabled = true;
            buttonStop.Enabled = false;
        }

        // 1 oct 22.5Hz to approx. 20000Hz ... 10 variations

        public bool Start() {
            m_inputChannelNum = listBoxInput.SelectedIndex;

            double startFreq = 22.5;
            double endFreq = 20000.0;
            int nSamples = SAMPLE_RATE;

            System.Diagnostics.Debug.Assert(SAMPLE_RATE <= nSamples);

            int nFreq = 0;
            for (double f = startFreq; f < endFreq; f *= Math.Pow(2, 1.0 / 3.0)) {
                ++nFreq;
            }
            m_seconds = nFreq;

            int[] outputData = new int[nFreq * nSamples];
            int pos = 0;
            for (double f = startFreq; f < endFreq; f *= Math.Pow(2, 1.0 / 3.0)) {
                for (int i = 0; i < nSamples; ++i) {
                    outputData[pos + i] = 0;
                }

                for (int i = 0; i < SAMPLE_RATE * (int)numericUpDownPulseCount.Value / f; ++i) {
                    outputData[pos + i] = (int)(System.Int32.MaxValue * Math.Sin(2.0 * Math.PI * (i * f / SAMPLE_RATE)));
                }
                pos += nSamples;
            }
            foreach (int idx in listBoxOutput.SelectedIndices) {
                asio.OutputSet(idx, outputData, false);
            }
            asio.InputSet(listBoxInput.SelectedIndex, outputData.Length);

            if (0 <= listBoxClockSources.SelectedIndex) {
                asio.ClockSourceSet(listBoxClockSources.SelectedIndex);
            }

            asio.Start();

            progressBar1.Value = 0;
            progressBar1.Visible = true;

            bw = new BackgroundWorker();
            bw.WorkerReportsProgress = true;
            bw.DoWork             += new DoWorkEventHandler(DoWork);
            bw.ProgressChanged    += new ProgressChangedEventHandler(ProgressChanged);
            bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(RunWorkerCompleted);
            bw.RunWorkerAsync();
            buttonStop.Enabled = true;
            return true;
        }

        private void buttonStart_Click(object sender, EventArgs e) {
            m_writeFilePath = textBoxFilePath.Text;
            buttonStart.Enabled = false;
            Start();
        }

        private void buttonBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.FileName = textBoxFilePath.Text;
            ofd.CheckPathExists = true;
            ofd.CheckFileExists = false;
            if (DialogResult.OK == ofd.ShowDialog()) {
                textBoxFilePath.Text = ofd.FileName;
            }
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            buttonStop.Enabled = false;
            asio.Stop();
        }

        private void buttonAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(string.Format("Pulse5 by Yamamoto Software Lab.\n\n{0}",asio.AsioTrademarkStringGet()));
        }

        private void buttonControlPanel_Click(object sender, EventArgs e) {
            int rv = asio.ControlPanel();
            if (-1000 == rv) {
                MessageBox.Show(string.Format("Control panel is not present on this device.", rv));
            }
        }
    }
}

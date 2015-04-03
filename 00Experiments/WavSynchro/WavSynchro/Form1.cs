using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using WavDiff;

namespace WavSynchro
{
    public partial class Form1 : Form
    {
        private System.Resources.ResourceManager rm;

        public Form1()
        {
            InitializeComponent();

            rm = WavSynchro.Properties.Resources.ResourceManager;
            textBoxConsole.Text = rm.GetString("Introduction") + "\r\n";
            GuiStatusUpdate();
        }

        private void GuiStatusUpdate()
        {
            if (string.Empty != textBoxWavFile1.Text &&
                string.Empty != textBoxWavFile2.Text) {
                buttonStart.Enabled = true;
            } else {
                buttonStart.Enabled = false;
            }
        }

        private string OpenDialogAndAskPath(bool bReadFile)
        {
            string ret = string.Empty;

            using (OpenFileDialog ofd = new OpenFileDialog()) {
                ofd.ReadOnlyChecked = bReadFile;
                ofd.Multiselect = false;
                ofd.Filter = rm.GetString("WavFileFilter");
                ofd.CheckPathExists = bReadFile;
                ofd.CheckFileExists = bReadFile;
                ofd.AutoUpgradeEnabled = true;
                DialogResult dr = ofd.ShowDialog();
                if (DialogResult.OK == dr) {
                    ret = ofd.FileName;
                }
            }

            return ret;
        }

        private string ReadFilePathToWriteFilePath(string path)
        {
            return path + "_s.wav";
        }

        private void buttonBrowseWavFile1_Click(object sender, EventArgs e)
        {
            string path = OpenDialogAndAskPath(true);
            if (string.Empty != path) {
                textBoxWavFile1.Text = path;
                GuiStatusUpdate();
                textBoxConsole.Text += string.Format(rm.GetString("WavFileSpecified"), 1, path, ReadFilePathToWriteFilePath(path)) + "\r\n";
            }
        }

        private void buttonBrowseWavFile2_Click(object sender, EventArgs e)
        {
            string path = OpenDialogAndAskPath(true);
            if (string.Empty != path) {
                textBoxWavFile2.Text = path;
                GuiStatusUpdate();
                textBoxConsole.Text += string.Format(rm.GetString("WavFileSpecified"), 2, path, ReadFilePathToWriteFilePath(path)) + "\r\n";
            }
        }

        struct VolumeInfo
        {
            public int delay;
            public long accumulatedDiff;
            public long w1Volume;
            public long w2Volume;
        }

        /** @param delay_return [out] (0 < delay_return) w1 is delayed by delay samples
         *                     (delay_return < 0) w2 is delayed by delay samples
         *  @param w1w2VolumeRatio_return [out] >1: w1 volume is larger than w2, <1: w1 volume is smaller than w2
         */
        private bool SampleDelay(WavData w1, WavData w2, out int delay_return, out double w1w2VolumeRatio_return)
        {
            float ACCUMULATE_SECONDS_MAX = (float)numericAccumulateSeconds.Value;
            float DELAY_SECONDS_MAX      = (float)numericStartDelayTorelance.Value;
            delay_return = 0;

            SortedDictionary<long, VolumeInfo> delayValueAndPos =
                new SortedDictionary<long, VolumeInfo>();

            int samplesPerSecond = w1.SampleRate;
            /* assume w1 is delayed (0 < delay) */
            for (int delay=0; delay < samplesPerSecond * DELAY_SECONDS_MAX; ++delay) {
                VolumeInfo vi = new VolumeInfo();
                vi.delay = delay;

                for (int pos=0; pos < samplesPerSecond * ACCUMULATE_SECONDS_MAX; ++pos) {
                    int w1Value = Math.Abs(w1.Sample16Get(0, pos));
                    int w2Value = Math.Abs(w2.Sample16Get(0, pos + delay));
                    vi.w1Volume += w1Value;
                    vi.w2Volume += w2Value;
                    vi.accumulatedDiff += Math.Abs(w1Value - w2Value);
                }
                // Console.Write("[{0} {1}]", delay, acc);
                if (!delayValueAndPos.ContainsKey(vi.accumulatedDiff)) {
                    delayValueAndPos[vi.accumulatedDiff] = vi;
                }
                backgroundWorker1.ReportProgress(delay * 50 / (int)(samplesPerSecond * DELAY_SECONDS_MAX));
            }

            /* assume w2 is delayed (delay < 0) */
            for (int delay=1; delay < samplesPerSecond * DELAY_SECONDS_MAX; ++delay) {
                VolumeInfo vi = new VolumeInfo();
                vi.delay = -delay;

                for (int pos=0; pos < samplesPerSecond * ACCUMULATE_SECONDS_MAX; ++pos) {
                    int w1Value = Math.Abs(w1.Sample16Get(0, pos + delay));
                    int w2Value = Math.Abs(w2.Sample16Get(0, pos));
                    vi.w1Volume += w1Value;
                    vi.w2Volume += w2Value;
                    vi.accumulatedDiff += Math.Abs(w1Value - w2Value);
                }
                // Console.Write("[{0} {1}]", -delay, acc);
                if (!delayValueAndPos.ContainsKey(vi.accumulatedDiff)) {
                    delayValueAndPos[vi.accumulatedDiff] = vi;
                }
                backgroundWorker1.ReportProgress(50 + delay * 50 / (int)(samplesPerSecond * DELAY_SECONDS_MAX));
            }

            SortedDictionary<long, VolumeInfo>.Enumerator e = delayValueAndPos.GetEnumerator();
            e.MoveNext();

            w1w2VolumeRatio_return = (double)e.Current.Value.w1Volume / e.Current.Value.w2Volume;
            delay_return = e.Current.Value.delay;

            Console.WriteLine();
            Console.WriteLine(rm.GetString("SampleDelaySummary"),
                delay_return, (double)delay_return / samplesPerSecond,
                (double)e.Current.Key / (samplesPerSecond * DELAY_SECONDS_MAX),
                w1w2VolumeRatio_return);
            if (w1w2VolumeRatio_return < 0.5 || 2.0 < w1w2VolumeRatio_return) {
                return false;
            }
            return true;
        }

        string resultString = string.Empty;

        private bool SaveWav(int sampleRate, int bitsPerSample, List<PcmSamples1Channel> samples, string path)
        {
            bool ret = true;

            WavData wav = new WavData();
            wav.Create(sampleRate, bitsPerSample, samples);

            try {
                using (BinaryWriter bw = new BinaryWriter(File.Open(path, FileMode.CreateNew))) {
                    wav.Write(bw);
                }
            } catch (Exception ex) {
                resultString = ex.ToString();
                ret = false;
            }
            return ret;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int sampleDelay;
            double w1w2VolumeRatio;
            if (!SampleDelay(wavRead1, wavRead2, out sampleDelay, out w1w2VolumeRatio)) {
                e.Result = false;
                resultString = rm.GetString("TwoWavFilesTooDifferent");
                return;
            }

            int numSamples = wavRead1.NumSamples - 2 * Math.Abs(sampleDelay);
            if (wavRead2.NumSamples < wavRead1.NumSamples) {
                numSamples = wavRead2.NumSamples - 2 * Math.Abs(sampleDelay);
            }

            List<PcmSamples1Channel> samples1 = new List<PcmSamples1Channel>();
            for (int i=0; i < wavRead1.NumChannels; ++i) {
                PcmSamples1Channel ps = new PcmSamples1Channel(numSamples, wavRead1.BitsPerSample);
                samples1.Add(ps);
            }
            List<PcmSamples1Channel> samples2 = new List<PcmSamples1Channel>();
            for (int i=0; i < wavRead2.NumChannels; ++i) {
                PcmSamples1Channel ps = new PcmSamples1Channel(numSamples, wavRead2.BitsPerSample);
                samples2.Add(ps);
            }

            long acc = 0;
            int maxDiff = 0;
            int maxDiffPos = 0;
            double maxDiffRatio = 0;

            if (0 <= sampleDelay) {
                for (int ch=0; ch < wavRead1.NumChannels; ++ch) {
                    PcmSamples1Channel ps1 = samples1[ch];
                    PcmSamples1Channel ps2 = samples2[ch];
                    for (int sample=0; sample < numSamples; ++sample) {
                        int diff = (int)(wavRead1.Sample16Get(ch, sample)
                                       - wavRead2.Sample16Get(ch, sample + sampleDelay));

                        int absDiff = Math.Abs(diff);
                        acc += absDiff;
                        if (maxDiff < absDiff) {
                            maxDiff = absDiff;
                            maxDiffRatio = (double)wavRead1.Sample16Get(ch, sample) /
                                            wavRead2.Sample16Get(ch, sample + sampleDelay);
                            maxDiffPos = sample;
                        }

                        ps1.Set16(sample, wavRead1.Sample16Get(ch, sample));
                        ps2.Set16(sample, wavRead2.Sample16Get(ch, sample + sampleDelay));
                    }
                }
            } else {
                // sampleDelay < 0
                for (int ch=0; ch < wavRead1.NumChannels; ++ch) {
                    PcmSamples1Channel ps1 = samples1[ch];
                    PcmSamples1Channel ps2 = samples2[ch];
                    for (int sample=0; sample < numSamples; ++sample) {
                        int diff = (int)(wavRead1.Sample16Get(ch, sample - sampleDelay)
                                       - wavRead2.Sample16Get(ch, sample));

                        int absDiff = Math.Abs(diff);
                        acc += absDiff;
                        if (maxDiff < absDiff) {
                            maxDiff = absDiff;
                            maxDiffRatio = (double)wavRead1.Sample16Get(ch, sample) /
                                            wavRead2.Sample16Get(ch, sample + sampleDelay);
                            maxDiffPos = sample;
                        }

                        ps1.Set16(sample, wavRead1.Sample16Get(ch, sample - sampleDelay));
                        ps2.Set16(sample, wavRead2.Sample16Get(ch, sample));
                    }
                }
            }

            if (0 == acc) {
                e.Result = false;
                resultString = rm.GetString("TwoWavFilesAreExactlyTheSame");
                return;
            }

            if (0 < maxDiff) {
                int maxMagnitude = 32767 / maxDiff;
                resultString = string.Format(rm.GetString("DiffStatistics"),
                    (double)acc / numSamples, maxDiff, maxDiffPos, (double)maxDiffPos/wavRead1.SampleRate, 20.0 * System.Math.Log10(maxDiffRatio));
                Console.WriteLine(resultString);
            }

            if (!SaveWav(wavRead1.SampleRate, wavRead1.BitsPerSample, samples1, ReadFilePathToWriteFilePath(textBoxWavFile1.Text))) {
                e.Result = false;
                return;
            }
            if (!SaveWav(wavRead2.SampleRate, wavRead2.BitsPerSample, samples2, ReadFilePathToWriteFilePath(textBoxWavFile2.Text))) {
                e.Result = false;
                return;
            }
            e.Result = true;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (false == (bool)e.Result) {
                textBoxConsole.Text += resultString + "\r\n";
                textBoxConsole.Text += rm.GetString("WriteFailed") + "\r\n";
            } else {
                textBoxConsole.Text += resultString + "\r\n";
                textBoxConsole.Text += rm.GetString("WriteSucceeded") + "\r\n";
            }

            progressBar1.Value = 0;
            buttonStart.Enabled = true;
        }

        private WavData ReadWavFile(string path)
        {
            WavData wavData = new WavData();

            Console.WriteLine(rm.GetString("ReadFileStarted"), path);

            using (BinaryReader br1 = new BinaryReader(File.Open(path, FileMode.Open))) {
                if (!wavData.Read(br1)) {
                    textBoxConsole.Text += string.Format(rm.GetString("ReadFileFailFormat"), path) + "\r\n";
                    return null;
                }
                if (16 != wavData.BitsPerSample) {
                    textBoxConsole.Text += string.Format(rm.GetString("BitsPerSampleFailFormat"), path, wavData.BitsPerSample) + "\r\n";
                    return null;
                }

                if (wavData.NumSamples < wavData.SampleRate * (double)numericStartDelayTorelance.Value + (double)numericAccumulateSeconds.Value) {
                    textBoxConsole.Text += string.Format(rm.GetString("WavFileTooShort"), path, (double)numericStartDelayTorelance.Value + (double)numericAccumulateSeconds.Value) + "\r\n";
                    return null;
                }
            }

            return wavData;
        }

        WavData wavRead1;
        WavData wavRead2;

        private void buttonStart_Click(object sender, EventArgs e)
        {
            textBoxConsole.Clear();

            textBoxConsole.Text += string.Format(rm.GetString("ProcessStarted"),
                textBoxWavFile1.Text, ReadFilePathToWriteFilePath(textBoxWavFile1.Text),
                textBoxWavFile2.Text, ReadFilePathToWriteFilePath(textBoxWavFile2.Text)) + "\r\n";

            wavRead1 = ReadWavFile(textBoxWavFile1.Text);
            if (null == wavRead1) {
                textBoxConsole.Text += string.Format(rm.GetString("ReadWavFileFailed"), textBoxWavFile1.Text) + "\r\n";
                return;
            }

            wavRead2 = ReadWavFile(textBoxWavFile2.Text);
            if (null == wavRead2) {
                textBoxConsole.Text += string.Format(rm.GetString("ReadWavFileFailed"), textBoxWavFile2.Text) + "\r\n";
                return;
            }

            if (wavRead1.SampleRate != wavRead2.SampleRate) {
                textBoxConsole.Text += string.Format(rm.GetString("SampleRateIsDifferent")) + "\r\n";
                return;
            }
            if (wavRead1.NumChannels != wavRead2.NumChannels) {
                textBoxConsole.Text += string.Format(rm.GetString("NumChannelsIsDifferent")) + "\r\n";
                return;
            }

            backgroundWorker1.RunWorkerAsync();
            buttonStart.Enabled = false;

        }
    }
}

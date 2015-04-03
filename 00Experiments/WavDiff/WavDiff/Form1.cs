/*
    WavDiff
    Copyright (C) 2009 Yamamoto DIY Software Lab.

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/
using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

namespace WavDiff
{
    public partial class Form1 : Form
    {
        private System.Resources.ResourceManager rm;

        private double DelaySecondsMax
        {
            get { return (double)numericToleranceSeconds.Value; }
        }
        private double AccumulateSecondsMax
        {
            get { return (double)numericAccumulateSeconds.Value; }
        }
        private double Magnitude
        {
            get { return (double)numericMagnitude.Value; }
        }

        private void GuiStatusUpdate()
        {
            if (string.Empty != textBoxRead1.Text &&
                string.Empty != textBoxRead2.Text &&
                string.Empty != textBoxWrite.Text) {
                buttonStart.Enabled = true;
            } else {
                buttonStart.Enabled = false;
            }
        }

        public Form1()
        {
            InitializeComponent();

            rm = WavDiff.Properties.Resources.ResourceManager;
            textBoxConsole.Text = rm.GetString("Introduction") + "\r\n";
            GuiStatusUpdate();

            Console.WriteLine(rm.GetString("ConsoleIntroduction"));
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

        private void buttonRead1_Click(object sender, EventArgs e)
        {
            string path = OpenDialogAndAskPath(true);
            if (string.Empty != path) {
                textBoxRead1.Text = path;
                GuiStatusUpdate();
            }
        }

        private void buttonRead2_Click(object sender, EventArgs e)
        {
            string path = OpenDialogAndAskPath(true);
            if (string.Empty != path) {
                textBoxRead2.Text = path;
                GuiStatusUpdate();
            }
        }

        private void buttonWrite_Click(object sender, EventArgs e)
        {
            string path = OpenDialogAndAskPath(false);
            if (string.Empty != path) {
                textBoxWrite.Text = path;
                GuiStatusUpdate();
            }
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

                if (wavData.NumSamples < wavData.SampleRate * DelaySecondsMax + AccumulateSecondsMax) {
                    textBoxConsole.Text += string.Format(rm.GetString("WavFileTooShort"), path, DelaySecondsMax + AccumulateSecondsMax) + "\r\n";
                    return null;
                }
            }

            return wavData;
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
            float ACCUMULATE_SECONDS_MAX = (float)AccumulateSecondsMax;
            float DELAY_SECONDS_MAX      = (float)DelaySecondsMax;
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

        WavData wavRead1;
        WavData wavRead2;

        private void buttonStart_Click(object sender, EventArgs e)
        {
            textBoxConsole.Text += string.Format(rm.GetString("ProcessStarted"),
                textBoxRead1.Text, textBoxRead2.Text, Magnitude, textBoxWrite.Text) + "\r\n";

            wavRead1 = ReadWavFile(textBoxRead1.Text);
            if (null == wavRead1) {
                textBoxConsole.Text += string.Format(rm.GetString("ReadWavFileFailed"), textBoxRead1.Text) + "\r\n";
                return;
            }

            wavRead2 = ReadWavFile(textBoxRead2.Text);
            if (null == wavRead2) {
                textBoxConsole.Text += string.Format(rm.GetString("ReadWavFileFailed"), textBoxRead2.Text) + "\r\n";
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

        string resultString = string.Empty;

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            int sampleDelay;
            double w1w2VolumeRatio;
            if (!SampleDelay(wavRead1, wavRead2, out sampleDelay, out w1w2VolumeRatio)) {
                e.Result = false;
                resultString = rm.GetString("TwoWavFilesTooDifferent");
                return;
            }

            if (!checkBoxAutoAdjustVolumeDifference.Checked) {
                w1w2VolumeRatio = 1.0;
            }

            int numSamples = wavRead1.NumSamples - 2 * Math.Abs(sampleDelay);
            if (wavRead2.NumSamples < wavRead1.NumSamples) {
                numSamples = wavRead2.NumSamples - 2 * Math.Abs(sampleDelay);
            }

            List<PcmSamples1Channel> samples = new List<PcmSamples1Channel>();
            for (int i=0; i<wavRead1.NumChannels; ++i) {
                PcmSamples1Channel ps = new PcmSamples1Channel(numSamples, wavRead1.BitsPerSample);
                samples.Add(ps);
            }

            long acc = 0;
            int maxDiff = 0;

            float magnitude = (float)Magnitude;
            if (0 <= sampleDelay) {
                for (int ch=0; ch < wavRead1.NumChannels; ++ch) {
                    PcmSamples1Channel ps = samples[ch];
                    for (int sample=0; sample < numSamples; ++sample) {
                        int diff = (int)(wavRead1.Sample16Get(ch, sample)
                                       - wavRead2.Sample16Get(ch, sample + sampleDelay) * w1w2VolumeRatio);

                        int absDiff = Math.Abs(diff);
                        acc += absDiff;
                        if (maxDiff < absDiff) {
                            maxDiff = absDiff;
                        }

                        ps.Set16(sample, (short)(diff * magnitude));
                    }
                }
            } else {
                // sampleDelay < 0
                for (int ch=0; ch < wavRead1.NumChannels; ++ch) {
                    PcmSamples1Channel ps = samples[ch];
                    for (int sample=0; sample < numSamples; ++sample) {
                        int diff = (int)(wavRead1.Sample16Get(ch, sample - sampleDelay)
                                       - wavRead2.Sample16Get(ch, sample) * w1w2VolumeRatio);

                        int absDiff = Math.Abs(diff);
                        acc += absDiff;
                        if (maxDiff < absDiff) {
                            maxDiff = absDiff;
                        }

                        ps.Set16(sample, (short)(diff * magnitude));
                    }
                }
            }

            if (0 == acc) {
                e.Result = false;
                resultString = rm.GetString("TwoWavFilesAreExactlyTheSame");
                return;
            }

            WavData wav = new WavData();
            wav.Create(wavRead1.SampleRate, wavRead1.BitsPerSample, samples);

            if (0 < maxDiff) {
                int maxMagnitude = 32767 / maxDiff;
                resultString = string.Format(rm.GetString("DiffStatistics"),
                    (double)acc / wav.NumSamples, maxDiff, maxMagnitude);
                Console.WriteLine(resultString);

                if (32767 < maxDiff * magnitude) {
                    Console.WriteLine(rm.GetString("OutputFileLevelOver"), maxMagnitude); 
                }
            }

            try {
                using (BinaryWriter bw = new BinaryWriter(File.Open(textBoxWrite.Text, FileMode.CreateNew))) {
                    wav.Write(bw);
                }
            } catch (Exception ex) {
                resultString = ex.ToString();
                e.Result = false;
            }
            e.Result = true;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (false == (bool)e.Result) {
                textBoxConsole.Text += resultString + "\r\n";
                textBoxConsole.Text += string.Format(rm.GetString("WriteFailed"), textBoxWrite.Text) + "\r\n";
            } else {
                textBoxConsole.Text += resultString + "\r\n";
                textBoxConsole.Text += string.Format(rm.GetString("WriteSucceeded"), textBoxWrite.Text) + "\r\n";
            }

            progressBar1.Value = 0;
            buttonStart.Enabled = true;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        private void numericAccumulateSeconds_ValueChanged(object sender, EventArgs e)
        {
            GuiStatusUpdate();
        }

        private void numericAccumulateSeconds_KeyDown(object sender, KeyEventArgs e)
        {
            GuiStatusUpdate();
        }

        private void numericAccumulateSeconds_KeyPress(object sender, KeyPressEventArgs e)
        {
            GuiStatusUpdate();
        }

        private void numericAccumulateSeconds_KeyUp(object sender, KeyEventArgs e)
        {
            GuiStatusUpdate();
        }

        private void numericMagnitude_KeyDown(object sender, KeyEventArgs e)
        {
            GuiStatusUpdate();
        }

        private void numericMagnitude_KeyPress(object sender, KeyPressEventArgs e)
        {
            GuiStatusUpdate();
        }

        private void numericMagnitude_KeyUp(object sender, KeyEventArgs e)
        {
            GuiStatusUpdate();
        }

        private void numericMagnitude_ValueChanged(object sender, EventArgs e)
        {
            GuiStatusUpdate();
        }
    }
}

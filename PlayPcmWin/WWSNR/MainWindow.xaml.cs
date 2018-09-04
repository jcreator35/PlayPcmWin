using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using WWAudioFilterCore;

namespace WWSNR {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private bool mInitialized = false;

        enum WeightingType {
            AWeighting,
            ITUR4684,
        };

        public MainWindow() {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mInitialized = true;
            TextBoxUpdated();
        }

        private string OpenFileDialogAndAsk(string s) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = WWAudioFilterCore.Properties.Resources.FilterReadAudioFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return s;
            }
            return dlg.FileName;
        }

        private void buttonBrowse_Click(object sender, RoutedEventArgs e) {
            textBoxSFile.Text = OpenFileDialogAndAsk(textBoxSFile.Text);
            TextBoxUpdated();
        }

        private void buttonBrowseSN_Click(object sender, RoutedEventArgs e) {
            textBoxSNFile.Text = OpenFileDialogAndAsk(textBoxSNFile.Text);
            TextBoxUpdated();
        }

        private void TextBoxUpdated() {
            bool bReady = false;
            try {
                if (textBoxSFile.Text.Length != 0 && System.IO.File.Exists(textBoxSFile.Text)
                        && textBoxSNFile.Text.Length != 0 && System.IO.File.Exists(textBoxSNFile.Text)) {
                    bReady = true;
                }
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }

            if (bReady) {
                buttonProcess.IsEnabled = true;
            } else {
                buttonProcess.IsEnabled = false;
            }
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        private void AddLog(string s) {
            textBoxLog.Text += s;
            textBoxLog.ScrollToEnd();
        }

        BackgroundWorker mBW;

        class BWArgs {
            public WeightingType wt;
            public string sFile;
            public string snFile;
            public BWArgs(WeightingType aWT, string aS, string aSN) {
                wt = aWT;
                sFile = aS;
                snFile = aSN;
            }
        };

        class BWResult {
            public WeightingType wt;
            public string sFile;
            public string snFile;
            public string result;
            public List<double> snr = new List<double>();
            public BWResult(WeightingType aWT, string aS, string aSN, string r) {
                wt = aWT;
                sFile = aS;
                snFile = aSN;
                result = r;
            }
        };

        private void buttonProcess_Click(object sender, RoutedEventArgs e) {
            AddLog("Process started.\n");

            var wt = WeightingType.AWeighting;
            if (radioButton468Curve.IsChecked==true) {
                wt = WeightingType.ITUR4684;
            }

            groupBoxSettings.IsEnabled = false;
            buttonProcess.IsEnabled = false;

            mBW = new BackgroundWorker();
            mBW.DoWork += new DoWorkEventHandler(mBW_DoWork);
            mBW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mBW_RunWorkerCompleted);
            mBW.WorkerReportsProgress = true;
            mBW.ProgressChanged += new ProgressChangedEventHandler(mBW_ProgressChanged);
            mBW.RunWorkerAsync(new BWArgs(wt, textBoxSFile.Text, textBoxSNFile.Text));
        }

        void mBW_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progressBar1.Value = e.ProgressPercentage;
        }

        void mBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            var r = e.Result as BWResult;

            for (int ch = 0; ch < r.snr.Count; ++ch) {
                AddLog(string.Format("    S File = {0}\n", r.sFile));
                AddLog(string.Format("    S+N File = {0}\n", r.snFile));

                AddLog(string.Format("    Weighting type = {0}\n", r.wt));
                if (r.snr[ch] == 0) {
                    AddLog(string.Format("    SNR of ch{0} = Undetermined\n", ch + 1));
                } else {
                    double snr = 20 * Math.Log10(r.snr[ch]);
                    AddLog(string.Format("    SNR of ch{0} = {1:0.00} dB\n", ch + 1, snr));
                }
            }

            AddLog(r.result);

            AddLog("\n");

            progressBar1.Value = 0;
            groupBoxSettings.IsEnabled = true;
            buttonProcess.IsEnabled = true;
        }

        private long mLastReport;

        /// <summary>
        /// 1秒に1回以上の頻度でBackgroundWorker.ReportProgressすると詰まるので呼び出し頻度を制限する。
        /// </summary>
        void ReportProgress(BackgroundWorker bw, Stopwatch sw, int percent) {
            if (sw.ElapsedMilliseconds - mLastReport < 1000) {
                return;
            }

            bw.ReportProgress(percent);
            mLastReport = sw.ElapsedMilliseconds;
        }

        void mBW_DoWork(object sender, DoWorkEventArgs e) {
            var bw = sender as BackgroundWorker;
            var args = e.Argument as BWArgs;
            var r = new BWResult(args.wt, args.sFile, args.snFile, "Process succeeded.\n");
            e.Result = r;

            var sw = new Stopwatch();
            sw.Start();
            mLastReport = sw.ElapsedMilliseconds;

            AudioData audioS;
            AudioData audioSN;

            try {

                bw.ReportProgress(10);

                if (0 != WWAudioFilterCore.AudioDataIO.Read(args.sFile, out audioS)) {
                    r.result = string.Format("Error: signal file read error. {0}", textBoxSFile.Text);
                    return;
                }

                ReportProgress(bw, sw, 20);

                if (0 != WWAudioFilterCore.AudioDataIO.Read(args.snFile, out audioSN)) {
                    r.result = string.Format("Error: signal+noise file read error. {0}", textBoxSNFile.Text);
                    return;
                }

                ReportProgress(bw, sw, 30);

                int fs = audioS.meta.sampleRate;
                if (fs != audioSN.meta.sampleRate) {
                    r.result = string.Format("Error: signal file and signal+noise file sample rate mismatch! {0} vs {1}",
                        audioS.meta.sampleRate, audioSN.meta.sampleRate);
                    return;
                }

                if (audioS.pcm.Count != audioSN.pcm.Count) {
                    r.result = string.Format("Error: signal file and signal+noise file channel count mismatch! {0} vs {1}",
                        audioS.pcm.Count, audioSN.pcm.Count);
                    return;
                }

                int numCh = audioS.pcm.Count;

                if (audioS.pcm[0].mTotalSamples != audioSN.pcm[0].mTotalSamples) {
                    r.result = string.Format("Error: signal file and signal+noise file sample count mismatch! {0} vs {1}",
                        audioS.pcm[0].mTotalSamples, audioSN.pcm[0].mTotalSamples);
                    return;
                }

                FilterBase filterS;
                FilterBase filterSN;

                switch (args.wt) {
                case WeightingType.AWeighting:
                    filterS = new AWeightingFilter(fs);
                    filterSN = new AWeightingFilter(fs);
                    break;
                case WeightingType.ITUR4684:
                    filterS = new ITUR4684WeightingFilter(fs);
                    filterSN = new ITUR4684WeightingFilter(fs);
                    break;
                default:
                    throw new NotImplementedException("Weighting Type");
                }

                int step = (int)audioS.pcm[0].mTotalSamples / 100;
                if (step <= 0) {
                    step = 1;
                }

                for (int ch = 0; ch < numCh; ++ch) {
                    double noiseSum = 0;
                    double signalSum = 0;

                    filterS.FilterStart();
                    filterSN.FilterStart();

                    long pos = 0;
                    long remain = audioS.pcm[ch].mTotalSamples;
                    while (0 < remain) {
                        int n = step;
                        if (remain - n < 0) {
                            n = (int)remain;
                        }
                        remain -= n;

                        var sIn = audioS.pcm[ch].GetPcmInDoublePcm(n);
                        var snIn = audioSN.pcm[ch].GetPcmInDoublePcm(n);

                        var sf = filterS.FilterDo(new WWUtil.LargeArray<double>(sIn));
                        var snf = filterSN.FilterDo(new WWUtil.LargeArray<double>(snIn));

                        pos += n;

                        for (int i = 0; i < n; ++i) {
                            double signalAbs = Math.Abs(sf.At(i));

                            signalSum += signalAbs;
                            double diff = Math.Abs(sf.At(i) - snf.At(i));
                            noiseSum += diff;
                        }

                        int percent = (int)(30 + 70 * (ch + (double)pos / audioS.pcm[0].mTotalSamples) / numCh);
                        ReportProgress(bw, sw, percent);
                    }

                    if (noiseSum <= 0.00000001f) {
                        r.snr.Add(0);
                    } else {
                        r.snr.Add(signalSum / noiseSum);
                    }

                    filterSN.FilterEnd();
                    filterS.FilterEnd();
                }
            } catch (Exception ex) {
                r.result = ex.ToString();
            }

            sw.Stop();
            sw = null;

            bw.ReportProgress(100);
        }

    }
}

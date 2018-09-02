using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WWAudioFilterCore;
using WWFlacRWCS;
using System.ComponentModel;

namespace WWSNR {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private bool mInitialized = false;

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
                buttonStart.IsEnabled = true;
            } else {
                buttonStart.IsEnabled = false;
            }
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        private void AddLog(string s) {
            textBoxLog.Text += s;
            textBoxLog.ScrollToEnd();
        }

        BackgroundWorker mBW;

        class BWArgs {
            public string sFile;
            public string snFile;
            public BWArgs(string aS, string aSN) {
                sFile = aS;
                snFile = aSN;
            }
        };

        class BWResult {
            public string result;
            public List<double> snr = new List<double>();
            public BWResult(string r) {
                result = r;
            }
        };

        private void buttonStart_Click(object sender, RoutedEventArgs e) {
            AddLog("Started\n");

            mBW = new BackgroundWorker();
            mBW.DoWork += new DoWorkEventHandler(mBW_DoWork);
            mBW.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mBW_RunWorkerCompleted);
            mBW.WorkerReportsProgress = true;
            mBW.ProgressChanged += new ProgressChangedEventHandler(mBW_ProgressChanged);
            mBW.RunWorkerAsync(new BWArgs(textBoxSFile.Text, textBoxSNFile.Text));
        }

        void mBW_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            progressBar1.Value = e.ProgressPercentage;
        }

        void mBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            var r = e.Result as BWResult;

            for (int ch = 0; ch < r.snr.Count; ++ch) {
                if (r.snr[ch] == 0) {
                    AddLog(string.Format("    SNR of ch{0} = Undetermined\n", ch + 1));
                } else {
                    double snr = 20 * Math.Log10(r.snr[ch]);
                    AddLog(string.Format("    SNR of ch{0} = {1:0.000} dB\n", ch + 1, snr));
                }
            }

            AddLog(r.result);

            progressBar1.Value = 0;
        }

        void mBW_DoWork(object sender, DoWorkEventArgs e) {
            var bw = sender as BackgroundWorker;
            var args = e.Argument as BWArgs;
            var r = new BWResult("Process succeeded.\n");
            e.Result = r;

            AudioData audioS;
            AudioData audioSN;

            try {

                bw.ReportProgress(10);

                if (0 != WWAudioFilterCore.AudioDataIO.Read(args.sFile, out audioS)) {
                    r.result = string.Format("Error: signal file read error. {0}", textBoxSFile.Text);
                    return;
                }

                bw.ReportProgress(30);

                if (0 != WWAudioFilterCore.AudioDataIO.Read(args.snFile, out audioSN)) {
                    r.result = string.Format("Error: signal+noise file read error. {0}", textBoxSNFile.Text);
                    return;
                }

                bw.ReportProgress(50);

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

                var filterS = new AWeightingFilter(fs);
                var filterSN = new AWeightingFilter(fs);

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
                    }

                    if (noiseSum <= 0.00000001f) {
                        r.snr.Add(0);
                    } else {
                        r.snr.Add(signalSum / noiseSum);
                    }

                    filterSN.FilterEnd();
                    filterS.FilterEnd();

                    bw.ReportProgress(50 + 50 * (ch + 1) / numCh);
                }
            } catch (Exception ex) {
                r.result = ex.ToString();
            }
        }

    }
}

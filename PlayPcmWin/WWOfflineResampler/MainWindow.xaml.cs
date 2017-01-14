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
using System.ComponentModel;
using WWMath;
using WWIIRFilterDesign;
using WWUtil;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WWOfflineResampler {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private const double CUTOFF_GAIN_DB          = -1.0;
        private const double STOPBAND_RIPPLE_DB      = -110;
        private const double CUTOFF_RATIO_OF_NYQUIST = 0.9;

        private const int START_PERCENT = 5;
        private const int CONVERT_START_PERCENT = 10;
        private const int WRITE_START_PERCENT = 90;
        private const int FRAGMENT_SAMPLES = 4 * 1024 * 1024;

        private enum State {
            NotReady,
            Ready,
            ReadFile,
            FilterDesigned,
            Converting,
            WriteFile
        }

        private State mState = State.NotReady;
        private bool mInitialized = false;
        private BackgroundWorker mBw = new BackgroundWorker();
        private WWAnalogFilterDesign.AnalogFilterDesign mAfd;
        private WWIIRFilterDesign.ImpulseInvarianceMethod mIIRiim;

        private static int [] mTargetSampleRateList = {
            32000,
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
        };

        public MainWindow() {
            InitializeComponent();

            mPoleZeroPlotZ.Mode = WWUserControls.PoleZeroPlot.ModeType.ZPlane;
            mPoleZeroPlotZ.Update();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            mBw.DoWork += new DoWorkEventHandler(mBw_DoWork);
            mBw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(mBw_RunWorkerCompleted);
            mBw.WorkerReportsProgress = true;
            mBw.ProgressChanged += new ProgressChangedEventHandler(mBw_ProgressChanged);
            
            mInitialized = true;

            mState = State.Ready;
            Update();

            /* サンプルレートの比の計算のテスト。
            foreach (int a in mTargetSampleRateList) {
                foreach (int b in mTargetSampleRateList) {
                    long lcm = WWMath.Functions.LCM(a, b);
                    Console.WriteLine("LCM({0}, {1}) = {2},  {3}/{4}", a, b,
                        lcm, lcm/a, lcm/b);
                }
            }
            */

        }

        private void buttonBrowseInputFile_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = Properties.Resources.FilterReadAudioFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxInputFile.Text = dlg.FileName;
            InputFormUpdated();
        }

        private void buttonBrowseOutputFile_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.Filter = Properties.Resources.FilterWriteAudioFiles;
            dlg.ValidateNames = true;

            var result = dlg.ShowDialog();
            if (result != true) {
                return;
            }

            textBoxOutputFile.Text = dlg.FileName;
            InputFormUpdated();
        }

        private void InputFormUpdated() {
            if (0 < textBoxInputFile.Text.Length &&
                    0 < textBoxOutputFile.Text.Length) {
                mState = State.Ready;
            } else {
                mState = State.NotReady;
            }

            Update();
        }

        private void Update() {
            if (!mInitialized) {
                return;
            }

            switch (mState) {
            case State.NotReady:
                buttonStartConversion.IsEnabled = false;
                break;
            case State.Ready:
                buttonStartConversion.IsEnabled = true;
                break;
            case State.ReadFile:
            case State.FilterDesigned:
            case State.Converting:
            case State.WriteFile:
                buttonStartConversion.IsEnabled = false;
                break;
            }
        }

        class BWStartParams {
            public readonly string inputFile;
            public readonly int targetSampleRate;
            public readonly string outputFile;

            public BWStartParams(string aInputFile, int aTargetSampleRate, string aOutputFile) {
                inputFile = aInputFile;
                targetSampleRate = aTargetSampleRate;
                outputFile = aOutputFile;
            }
        };

        class BWProgressParam {
            public readonly State state;
            public readonly string message;
            public BWProgressParam(State aState, string aMessage) {
                state = aState;
                message = aMessage;
            }
        };

        class BWCompletedParam {
            public readonly int rv;
            public readonly string message;
            public BWCompletedParam(int aRv, string aMessage) {
                rv = aRv;
                message = aMessage;
            }
        }

        // FLACからPCMのバイト列を取得し、最大値1のdouble型のサンプル値の配列を作る。
        private double[] GetSamples(WWFlacRWCS.FlacRW flacR, WWFlacRWCS.Metadata meta,
                int ch, long posSamples, int nSamples) {
            int nBytes = nSamples * meta.bitsPerSample / 8;
            byte[] pcm;
            int rv = flacR.GetDecodedPcmBytes(ch, posSamples * meta.BytesPerSample,
                out pcm, nBytes);
            if (rv != nBytes) {
                return null;
            }
            double [] result = new double[nSamples];
            switch (meta.bitsPerSample) {
            case 16:
                for (int i = 0; i < nSamples; ++i) {
                    short v = (short)(pcm[i * 2] + (pcm[i * 2 + 1] << 8));
                    result[i] = v / 32768.0;
                }
                break;
            case 20:
            case 24:
                for (int i = 0; i < nSamples; ++i) {
                    int v = (int)((pcm[i * 3] << 8) + (pcm[i * 3 + 1] << 16) + (pcm[i * 3 + 2] << 24));
                    result[i] = v / 2147483648.0;
                }
                break;
            default:
                throw new NotSupportedException();
            }
            return result;
        }

        private byte[] ConvertTo24bitInt(double[] from) {
            byte[] to = new byte[from.Length * 3];
            for (int i = 0; i < from.Length; ++i) {
                int v = 0;
                if (from[i] < -1.0) {
                    v = -2147483648;
                } else if (2147483647.0 / 2147483648.0 < from[i]) {
                    v = 2147483647;
                } else {
                    v = (int)(from[i] * 2147483648.0);
                }

                to[i * 3 + 0] = (byte)(v >> 8);
                to[i * 3 + 1] = (byte)(v >> 16);
                to[i * 3 + 2] = (byte)(v >> 24);
            }
            return to;
        }

        private byte[] ConvertToIntegerPcm(double[] from, int bytesPerSample) {
            switch (bytesPerSample) {
            case 3:
                return ConvertTo24bitInt(from);
            default:
                // 作ってない。
                break;
            }
            return null;
        }

        private delegate void ProgressReportDelegate(int percent, BWProgressParam p);

        // decibel of field quantity (voltage)
        private static double FieldDecibelToRatio(double db) {
            return Math.Pow(10.0, db / 20.0);
        }

        private static double FieldQuantityToDecibel(double v) {
            return 20.0 * Math.Log10(v);
        }

        /// <summary>
        /// アップサンプル。インパルストレイン方式。
        /// </summary>
        private LargeArray<double> Upsample(double[] from, int scale) {
            var to = new LargeArray<double>(from.LongLength * scale);

            for (int i = 0; i < from.Length; ++i) {
                to.Set((long)i * scale, from[i]);
            }
            return to;
        }

        /// <summary>
        /// ダウンサンプル。デシメーション。
        /// </summary>
        private double[] Downsample(LargeArray<double> from, int scale) {
            var to = new double[from.LongLength / scale];

            for (int i = 0; i < to.Length; ++i) {
                to[i] = from.At((long)i * scale);
            }
            return to;
        }

        /// <summary>
        /// 音量調整。
        /// </summary>
        private double[] ApplyGain(double[] pcm, double gain) {
            for (int i = 0; i < pcm.Length; ++i) {
                pcm[i] = pcm[i] * gain;
            }
            return pcm;
        }

        private BWCompletedParam DoWork(BWStartParams param, ProgressReportDelegate ReportProgress) {
            int rv = 0;

            do {
                // FLACファイルからメタデータ、画像、音声を取り出す。
                WWFlacRWCS.Metadata metaR;
                var flacR = new WWFlacRWCS.FlacRW();
                rv = flacR.DecodeAll(param.inputFile);
                if (rv < 0) {
                    break;
                }

                flacR.GetDecodedMetadata(out metaR);

                byte [] picture = null;
                if (0 < metaR.pictureBytes) {
                    rv = flacR.GetDecodedPicture(out picture, metaR.pictureBytes);
                    if (rv < 0) {
                        break;
                    }
                }

                long lcm = WWMath.Functions.LCM(metaR.sampleRate, param.targetSampleRate);
                int upsampleScale = (int)(lcm/metaR.sampleRate);
                int downsampleScale = (int)(lcm/param.targetSampleRate);

                // IIRフィルターを設計。

                // ストップバンド最小周波数。
                double fs = metaR.sampleRate/2;
                if (param.targetSampleRate / 2 < fs) {
                    fs = param.targetSampleRate / 2;
                }
                double fc = fs * CUTOFF_RATIO_OF_NYQUIST;

                mAfd = new WWAnalogFilterDesign.AnalogFilterDesign();
                mAfd.DesignLowpass(0, CUTOFF_GAIN_DB, STOPBAND_RIPPLE_DB,
                    fc,
                    fs,
                    WWAnalogFilterDesign.AnalogFilterDesign.FilterType.Cauer,
                    WWAnalogFilterDesign.ApproximationBase.BetaType.BetaMax);

                var H_s = new List<FirstOrderRationalPolynomial>();
                for (int i = 0; i < mAfd.HPfdCount(); ++i) {
                    var p = mAfd.HPfdNth(i);
                    H_s.Add(p);
                }

                mIIRiim = new ImpulseInvarianceMethod(H_s, fc * 2.0 * Math.PI, lcm);


                ReportProgress(CONVERT_START_PERCENT, new BWProgressParam(State.FilterDesigned,
                    string.Format("Read FLAC completed.\nSource sample rate = {0}kHz.\nTarget sample rate = {1}kHz. ratio={2}/{3}\n",
                        metaR.sampleRate / 1000.0, param.targetSampleRate / 1000.0,
                        lcm/metaR.sampleRate, lcm/param.targetSampleRate)));

                // 書き込み準備。
                WWFlacRWCS.Metadata metaW = new WWFlacRWCS.Metadata(metaR);
                metaW.sampleRate = param.targetSampleRate;
                metaW.pictureBytes = metaR.pictureBytes;
                metaW.bitsPerSample = 24;
                metaW.totalSamples = metaR.totalSamples * upsampleScale / downsampleScale;

                var flacW = new WWFlacRWCS.FlacRW();
                flacW.EncodeInit(metaW);
                if (picture != null) {
                    flacW.EncodeSetPicture(picture);
                }

                var svs = new SampleValueStatistics();
                long totalSamplesToProcess = metaR.totalSamples * metaR.channels;
                long processedSampes = 0;

                // 変換する
                //for (int ch=0; ch<metaR.channels;++ch){
                Parallel.For(0, metaR.channels, (int ch) => {
                    var pcmW = new WWUtil.LargeArray<byte>(metaW.totalSamples * metaW.BytesPerSample);

                    // ローパスフィルターを作る。
                    var iirFilter = new IIRFilter();
#if true
                    for (int i = 0; i < mIIRiim.HzCount(); ++i) {
                        var p = mIIRiim.Hz(i);
                        iirFilter.Add(p);
                    }
#else
                    /*
                    HighOrderRationalPolynomial hor = new HighOrderRationalPolynomial(
                        new WWComplex[] { new WWComplex(0.0605, 0), new WWComplex(0.121, 0), new WWComplex(0.0605, 0) },
                        new WWComplex[] { new WWComplex(1, 0), new WWComplex(-1.194, 0), new WWComplex(0.436, 0) });
                    iirFilter.Add(hor);
                    */
                    iirFilter.Add(mIIRiim.HzCombined());
#endif
                    long remain = metaR.totalSamples;
                    long posY = 0;
                    for (long posX = 0; posX < metaR.totalSamples; posX += FRAGMENT_SAMPLES) {
                        int size = FRAGMENT_SAMPLES;
                        if (remain < size) {
                            size = (int)remain;
                        }

                        // 入力サンプル列x
                        var x = GetSamples(flacR, metaR, ch, posX, size);

                        // アップサンプルする。
                        var u = Upsample(x, upsampleScale);

                        // ローパスフィルターでエイリアシング雑音を除去する。
                        for (long i = 0; i < u.LongLength; ++i) {
                            double v = iirFilter.Filter(u.At(i));
                            u.Set(i, v);
                            svs.Add(v);
                        }

                        // ダウンサンプルする
                        var y = Downsample(u, downsampleScale);

                        // アップサンプル時に音量がupsampleScale分の1に下がっているのでupsampleScale倍する。
                        const double kSampleValueLimit = (double)8388607 / 8388608;
                        double gain = upsampleScale;
                        if (svs.MaxAbsValue() * gain < kSampleValueLimit) {
                            gain = kSampleValueLimit / svs.MaxAbsValue();
                        }
                        y = ApplyGain(y, gain);

                        // 出力する。
                        byte[] yPcm = ConvertToIntegerPcm(y, metaW.BytesPerSample);
                        pcmW.CopyFrom(yPcm, 0, posY, yPcm.Length);
                        posY += y.Length;

                        remain -= size;
                        processedSampes += size;

                        int percentage = (int)((long)CONVERT_START_PERCENT
                            + (WRITE_START_PERCENT - CONVERT_START_PERCENT)
                            * (processedSampes / totalSamplesToProcess));

                        ReportProgress(percentage, new BWProgressParam(State.Converting, ""));
                    }

                    rv = flacW.EncodeAddPcm(ch, pcmW);
                    System.Diagnostics.Debug.Assert(0 <= rv);
                });

                double maxMagnitudeDb = FieldQuantityToDecibel(svs.MaxAbsValue() * upsampleScale);
                ReportProgress(WRITE_START_PERCENT, new BWProgressParam(State.WriteFile,
                    string.Format("Maximum magnitude={0:G3}dBFS\nNow writing FLAC file {1}...\n", maxMagnitudeDb, param.outputFile)));
                rv = flacW.EncodeRun(param.outputFile);
                if (rv < 0) {
                    break;
                }

                flacW.EncodeEnd();
            } while (false);

            var cp = new BWCompletedParam(0, "");
            if (rv < 0) {
                cp = new BWCompletedParam(rv, WWFlacRWCS.FlacRW.ErrorCodeToStr(rv) + "\n");
            }
            return cp;
        }

        void mBw_DoWork(object sender, DoWorkEventArgs e) {
            var param = e.Argument as BWStartParams;
            var result = DoWork(param, (int percent, BWProgressParam p) => { mBw.ReportProgress(percent, p); });
            e.Result = result;
        }

        void mBw_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            var param = e.UserState as BWProgressParam;

            textBoxLog.Text += param.message;
            textBoxLog.ScrollToEnd();
            mState = param.state;
            progressBar1.Value = e.ProgressPercentage;

            if (mState == State.FilterDesigned) {
                mTimeDomainPlot.ImpulseResponseFunction = mAfd.ImpulseResponseFunction;
                mTimeDomainPlot.StepResponseFunction = mAfd.UnitStepResponseFunction;
                mTimeDomainPlot.TimeScale = mAfd.TimeDomainFunctionTimeScale;
                mTimeDomainPlot.Update();

                mPoleZeroPlotZ.Mode = WWUserControls.PoleZeroPlot.ModeType.ZPlane;
                mPoleZeroPlotZ.TransferFunction = mIIRiim.TransferFunction;
                mPoleZeroPlotZ.Update();

                mFrequencyResponseZ.Mode = WWUserControls.FrequencyResponse.ModeType.ZPlane;
                mFrequencyResponseZ.SamplingFrequency = mIIRiim.SamplingFrequency();
                mFrequencyResponseZ.TransferFunction = mIIRiim.TransferFunction;
                mFrequencyResponseZ.Update();
            }

            Update();
        }

        void mBw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            var param = e.Result as BWCompletedParam;
            mState = State.Ready;

            mStopwatch.Stop();
            textBoxLog.Text += param.message;
            textBoxLog.Text += string.Format("Finished. elapsed time: {0} sec\n", mStopwatch.ElapsedMilliseconds * 0.001);
            textBoxLog.ScrollToEnd();

            Update();
            progressBar1.Value = 0;
        }

        Stopwatch mStopwatch = new Stopwatch();

        private void buttonStart_Click(object sender, RoutedEventArgs e) {
            int targetSampleRate = mTargetSampleRateList[comboBoxTargetSampleRate.SelectedIndex];

            mState = State.ReadFile;
            Update();

            progressBar1.Value = START_PERCENT;

            mStopwatch.Reset();
            mStopwatch.Start();

            mBw.RunWorkerAsync(new BWStartParams(textBoxInputFile.Text, targetSampleRate, textBoxOutputFile.Text));
        }

    }
}

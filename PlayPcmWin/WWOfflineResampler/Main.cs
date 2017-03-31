using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using WWIIRFilterDesign;
using WWMath;

namespace WWOfflineResampler {
    class Main {

        /// <summary>
        /// true: ZOH
        /// false: インパルストレイン
        /// 比較するとZOHのほうがローノイズ。
        /// </summary>
        private const bool USE_ZOH_UPSAMPLE = true;

        private const double CUTOFF_GAIN_DB = -1.0;

        // -10 : 3次 ◎ (poles 対称)(zeroes 虚数1ペア)
        // -20 : 5次 (4次) ×
        // -30 : 5次 ◎ (poles 対称)
        // -40 : 7次 (6次) ×
        // -50 : 7次 ◎ (poles 対称) 
        // -60 : 9次 (8次) ×
        // -70 : 9次 ◎ (poles 対称)(zeroesすべて実数)
        // -80 : 11次 (10次) ×
        // -90 : 11次 ◎ (poles 対称)(zeroes 虚数1ペア)
        // -100 : 13次 (12次) ×
        // -110 : 13次 ◎ (poles 対称)(zeroesすべて実数)
        // -120 : 15次 (14次) ×
        private const double STOPBAND_RIPPLE_DB = -50;
        private const double CUTOFF_RATIO_OF_NYQUIST = 0.9;

        public const int START_PERCENT = 5;
        public const int CONVERT_START_PERCENT = 10;
        public const int WRITE_START_PERCENT = 90;

        private Stopwatch mSw = new Stopwatch();

        public enum State {
            Started,
            ReadFile,
            FilterDesigned,
            Converting,
            WriteFile,
            Finished
        }

        public delegate void ProgressReportDelegate(int percent, BWProgressParam p);

        private WWAnalogFilterDesign.AnalogFilterDesign mAfd;
        private WWIIRFilterDesign.ImpulseInvarianceMethod mIIRiim;

        public WWAnalogFilterDesign.AnalogFilterDesign Afd() {
            return mAfd;
        }
        public WWIIRFilterDesign.ImpulseInvarianceMethod IIRiim() {
            return mIIRiim;
        }

        public class BWStartParams {
            public readonly string inputFile;
            public readonly int targetSampleRate;
            public readonly string outputFile;

            public BWStartParams(string aInputFile, int aTargetSampleRate, string aOutputFile) {
                inputFile = aInputFile;
                targetSampleRate = aTargetSampleRate;
                outputFile = aOutputFile;
            }
        };

        public class BWProgressParam {
            public readonly State state;
            public readonly string message;
            public BWProgressParam(State aState, string aMessage) {
                state = aState;
                message = aMessage;
            }
        };

        public class BWCompletedParam {
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
            double[] result = new double[nSamples];
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
        
        // decibel of field quantity (voltage)
        private static double FieldDecibelToRatio(double db) {
            return Math.Pow(10.0, db / 20.0);
        }

        private static double FieldQuantityToDecibel(double v) {
            return 20.0 * Math.Log10(v);
        }

        public BWCompletedParam DoWork(BWStartParams param, ProgressReportDelegate ReportProgress) {
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

                byte[] picture = null;
                if (0 < metaR.pictureBytes) {
                    rv = flacR.GetDecodedPicture(out picture, metaR.pictureBytes);
                    if (rv < 0) {
                        break;
                    }
                }

                long lcm = WWMath.Functions.LCM(metaR.sampleRate, param.targetSampleRate);
                int upsampleScale = (int)(lcm / metaR.sampleRate);
                int downsampleScale = (int)(lcm / param.targetSampleRate);

                // IIRフィルターを設計。

                // ストップバンド最小周波数。
                double fs = metaR.sampleRate / 2;
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

                var H_s = new List<FirstOrderComplexRationalPolynomial>();
                for (int i = 0; i < mAfd.HPfdCount(); ++i) {
                    var p = mAfd.HPfdNth(i);
                    H_s.Add(p);
                }

                mIIRiim = new ImpulseInvarianceMethod(H_s, fc * 2.0 * Math.PI, lcm);

                ReportProgress(CONVERT_START_PERCENT, new BWProgressParam(State.FilterDesigned,
                    string.Format("Read FLAC completed.\nSource sample rate = {0}kHz.\nTarget sample rate = {1}kHz. ratio={2}/{3}\n",
                        metaR.sampleRate / 1000.0, param.targetSampleRate / 1000.0,
                        lcm / metaR.sampleRate, lcm / param.targetSampleRate)));

                mSw.Restart();

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
                long totalSamplesOfAllChannels = metaR.totalSamples * metaR.channels;
                long processedSamples = 0;

                // Mathematica用の設定。
                WWComplex.imaginaryUnit = "I";

                // 変換する
                //for (int ch=0; ch<metaR.channels;++ch){
                Parallel.For(0, metaR.channels, (int ch) => {
                    var pcmW = new WWUtil.LargeArray<byte>(metaW.totalSamples * metaW.BytesPerSample);

#if USE_ZOH_UPSAMPLE
                    // 零次ホールドのハイ落ち補償フィルター。
                    var zohCompensation = new WWIIRFilterDesign.ZohNosdacCompensation(33);
#endif

                    // ローパスフィルターを作る。
                    // 実数係数版の多項式を使用。
                    var iirFilter = new IIRFilterSerial();
                    for (int i = 0; i < mIIRiim.RealHzCount(); ++i) {
                        var p = mIIRiim.RealHz(i);
                        Console.WriteLine("{0}", p.ToString("(z)^(-1)"));
                        iirFilter.Add(p);
                    }

                    long remainFrom = metaR.totalSamples;
                    long remainTo = metaW.totalSamples;
                    long posY = 0;
                    // 1秒分のバッファを処理する。
                    // 1単位で処理するサンプル数は、ソースのサンプルレートの倍数にすると
                    // 出力サンプル数がちょうど割り切れる。
                    for (long posX = 0; posX < metaR.totalSamples; posX += metaR.sampleRate) {
                        int sizeFrom = metaR.sampleRate;
                        if (remainFrom < sizeFrom) {
                            sizeFrom = (int)remainFrom;
                        }

                        // 入力サンプル列x
                        var x = GetSamples(flacR, metaR, ch, posX, sizeFrom);
                        int sizeTo = (int)((long)x.Length * upsampleScale / downsampleScale);
                        if (remainTo < sizeTo) {
                            sizeTo = (int)remainTo;
                        }

#if USE_ZOH_UPSAMPLE
                        if (1 < upsampleScale) {
                            // 零次ホールドでアップサンプルするのでハイ落ちを補償する。
                            x = zohCompensation.Filter(x);
                        }
#endif

                        // ローパスフィルターでエイリアシング雑音を除去しながらリサンプルする。
                        var y = new double[sizeTo];
                        for (long i = 0; i < x.Length * upsampleScale; ++i) {
#if USE_ZOH_UPSAMPLE        // 零次ホールド。
                            double v = x[i / upsampleScale];
#else                       // インパルストレイン。
                            double v = 0;
                            if ((i % upsampleScale) == 0) {
                                // インパルストレインアップサンプル時に音量が下がるのでupsampleScale倍する。
                                v = upsampleScale * x[i / upsampleScale];
                            }
#endif
                            v = iirFilter.Filter(v);
                            svs.Add(v);
                            if ((i % downsampleScale) == 0) {
                                int posTo = (int)(i / downsampleScale);
                                if (posTo < y.Length) {
                                    y[posTo] = v;
                                }
                            }
                        }

                        // 出力する。
                        byte[] yPcm = ConvertToIntegerPcm(y, metaW.BytesPerSample);
                        pcmW.CopyFrom(yPcm, 0, posY, yPcm.Length);
                        posY += yPcm.Length;

                        remainFrom -= sizeFrom;
                        remainTo   -= sizeTo;
                        processedSamples += sizeFrom;

                        int percentage = (int)(CONVERT_START_PERCENT
                            + (WRITE_START_PERCENT - CONVERT_START_PERCENT)
                            * ((double)processedSamples / totalSamplesOfAllChannels));

                        if (1000 < mSw.ElapsedMilliseconds) {
                            ReportProgress(percentage, new BWProgressParam(State.Converting, ""));
                            mSw.Restart();
                        }
                    }

                    rv = flacW.EncodeAddPcm(ch, pcmW);
                    System.Diagnostics.Debug.Assert(0 <= rv);
                });

                double maxMagnitudeDb = FieldQuantityToDecibel(svs.MaxAbsValue());
                string clippedMsg = "";
                if (0 <= maxMagnitudeDb) {
                    clippedMsg = "★★★ CLIPPED! ★★★";
                }
                ReportProgress(WRITE_START_PERCENT, new BWProgressParam(State.WriteFile,
                    string.Format("Maximum magnitude={0:G3}dBFS. {1}\nNow writing FLAC file {2}...\n",
                    maxMagnitudeDb, clippedMsg, param.outputFile)));
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

        private static readonly string COMMAND_TARGETSR = "-targetSR";

        private void PrintUsage(string programName) {
            Trace.WriteLine(string.Format("Commandline Usage: {0} -targetSR [samplerate] inputAudioFile outputAudioFile", programName));
        }

        public bool ParseCommandLine() {
            var argDictionary = new Dictionary<string, string>();

            var args = Environment.GetCommandLineArgs();
            if (5 != args.Length || !COMMAND_TARGETSR.Equals(args[1])) {
                PrintUsage(args[0]);
                return false;
            }

            string targetSRString = args[2];
            string inputFile = args[3];
            string outputFile = args[4];

            int targetSR = 0;
            if (!int.TryParse(targetSRString, out targetSR) || targetSR <= 0) {
                PrintUsage(args[0]);
                return false;
            }

            Trace.WriteLine(string.Format("target samplerate={0}kHz.\nProcessing...\n", targetSR * 0.001));

            var param = new BWStartParams(inputFile, targetSR, outputFile);
            var result = DoWork(param, (int percent, BWProgressParam p) => {
                Trace.WriteLine(string.Format("{0}% {1}", percent, p.message)); });

            if (0 < result.message.Length) {
                Trace.WriteLine(result.message);
            } else {
                Trace.WriteLine("Finished successfully.");
            }

            return true;
        }
    }
}

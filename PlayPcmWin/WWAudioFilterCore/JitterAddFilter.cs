// 日本語

using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;

namespace WWAudioFilterCore {
    public class JitterAddFilter : FilterBase {
        private RNGCryptoServiceProvider mRand = new RNGCryptoServiceProvider();

        // 入力パラメーター。

        /// <summary>
        /// 正弦波の時間ゆらぎ周波数(Hz)。
        /// たとえばリニア電源回路で60Hzを全波整流して平滑化するとDC電源電圧には120Hzの倍数の雑音が乗る。
        /// </summary>
        public double SineJitterFreq { get; set; }

        /// <summary>
        /// 時間ゆらぎの時間方向の振幅。ナノ秒。
        /// RMSで与える。最大幅はこの値の√2倍。
        /// </summary>
        public double SineJitterNanosec { get; set; }

        /// <summary>
        /// ランダムな雑音の量。Probability Density Function of Triangular Distribution。
        /// RMS
        /// </summary>
        public double TpdfJitterNanosec { get; set; }
        /// <summary>
        /// ランダムな雑音の量。Probability Density Function of Rectangular Distribution。
        /// RMS
        /// </summary>
        public double RpdfJitterNanosec { get; set; }

        /// <summary>
        /// 任意の時刻の波高値をSinc関数でリコンストラクションするとき、コンボリューションする幅。
        /// </summary>
        public int ConvolutionLengthMinus1 { get; set; }

        /// <summary>
        /// タイミングエラーのパターンを音声ファイルで与える。
        /// サンプル値が＋のとき、再サンプル時刻が未来の方向にずれる。
        /// サンプル値が－のとき、再サンプル時刻が過去の方向にずれる。
        /// 例えばピンクノイズのファイルを入力してピンクノイズのジッターを作ることができる。
        /// </summary>
        public string TimingErrorFile { get; set; }

        /// <summary>
        /// タイミングエラーファイルが最大振幅のとき、TimingErrorFileNanosec ナノ秒再サンプル時刻をずらす。
        /// </summary>
        public double TimingErrorFileNanosec { get; set; }

        // Setup()で計算する
        private long   mTotalSamples;
        private int    mSampleRate;
        private double mSineJitterAmp;
        private double mTpdfJitterAmp;
        private double mRpdfJitterAmp;

        private WWUtil.LargeArray<double> mTimingErrorFromAudioFile = null;

        // ジッターによって揺さぶられたクロックが生成した再サンプリング時刻。
        // PrepareResamplePosArray()で計算する
        private int[]    mResamplePosArray;
        private double[] mFractionArray;

        public JitterAddFilter(double sineJitterFreq, double sineJitterNanosec, double tpdfJitterNanosec,
                double rpdfJitterNanosec, int convolutionLengthMinus1, string timingErrorFile, double timingErrorFileNanosec)
                : base(FilterType.JitterAdd) {
            if (sineJitterFreq < 0) {
                throw new ArgumentOutOfRangeException("sineJitterFreq");
            }
            SineJitterFreq = sineJitterFreq;

            if (sineJitterNanosec < 0) {
                throw new ArgumentOutOfRangeException("sineJitterNanosec");
            }
            SineJitterNanosec = sineJitterNanosec;

            if (tpdfJitterNanosec < 0) {
                throw new ArgumentOutOfRangeException("tpdfJitterNanosec");
            }
            TpdfJitterNanosec = tpdfJitterNanosec;

            if (rpdfJitterNanosec < 0) {
                throw new ArgumentOutOfRangeException("rpdfJitterNanosec");
            }
            RpdfJitterNanosec = rpdfJitterNanosec;

            if (convolutionLengthMinus1 < 1024) {
                throw new ArgumentOutOfRangeException("convolutionLengthMinus1");
            }
            ConvolutionLengthMinus1 = convolutionLengthMinus1;

            TimingErrorFile = timingErrorFile;
            TimingErrorFileNanosec = timingErrorFileNanosec;
        }

        public override FilterBase CreateCopy() {
            return new JitterAddFilter(SineJitterFreq, SineJitterNanosec, TpdfJitterNanosec,
                    RpdfJitterNanosec, ConvolutionLengthMinus1, TimingErrorFile, TimingErrorFileNanosec);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterJitterAddDesc,
                    SineJitterFreq, SineJitterNanosec, TpdfJitterNanosec, RpdfJitterNanosec, ConvolutionLengthMinus1+1,
                    TimingErrorFileNanosec, TimingErrorFile);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4} {5} {6}",
                SineJitterFreq, SineJitterNanosec, TpdfJitterNanosec, RpdfJitterNanosec,
                ConvolutionLengthMinus1, TimingErrorFileNanosec, WWAFUtil.EscapeString(TimingErrorFile));
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 6 && tokens.Length != 8) {
                return null;
            }

            double sineJitterFreq;
            if (!Double.TryParse(tokens[1], out sineJitterFreq) || sineJitterFreq < 0) {
                return null;
            }

            double sineJitterNanosec;
            if (!Double.TryParse(tokens[2], out sineJitterNanosec) || sineJitterNanosec < 0) {
                return null;
            }

            double tpdfJitterNanosec;
            if (!Double.TryParse(tokens[3], out tpdfJitterNanosec) || tpdfJitterNanosec < 0) {
                return null;
            }

            double rpdfJitterNanosec;
            if (!Double.TryParse(tokens[4], out rpdfJitterNanosec) || rpdfJitterNanosec < 0) {
                return null;
            }

            int convolutionN;
            if (!Int32.TryParse(tokens[5], out convolutionN) || convolutionN < 1024) {
                return null;
            }

            double timingErrorNanosec = 0;
            string timingErrorFile = "";

            if (8 <= tokens.Length) {
                if (!Double.TryParse(tokens[6], out timingErrorNanosec)) {
                    return null;
                }
                timingErrorFile = tokens[7];
            }

            return new JitterAddFilter(sineJitterFreq, sineJitterNanosec, tpdfJitterNanosec,
                    rpdfJitterNanosec, convolutionN, timingErrorFile, timingErrorNanosec);
        }

        private static double
        SincD(double sinx, double x) {
            if (-2.2204460492503131e-016 < x && x < 2.2204460492503131e-016) {
                return 1.0;
            } else {
                return sinx / x;
            }
        }

        /// <summary>
        ///  仮数部が32bitぐらいまで値が埋まっているランダムの0～1
        /// </summary>
        /// <returns></returns>
        private static double GenRandom0to1(RNGCryptoServiceProvider gen) {
            byte[] bytes = new byte[4];
            gen.GetBytes(bytes);
            uint u = BitConverter.ToUInt32(bytes, 0);
            double d = (double)u / uint.MaxValue;
            return d;
        }

        private bool SetupTimingErrorFile() {
            if (TimingErrorFile == null || TimingErrorFile.Length == 0) {
                TimingErrorFileNanosec = 0.0;
                return true;
            }

            AudioData ad;
            int rv = AudioDataIO.Read(TimingErrorFile, out ad);
            if (rv < 0) {
                return false;
            }

            if (ad.meta.totalSamples < mTotalSamples) {
                MessageBox.Show(Properties.Resources.ErrorTimingErrorFile);
                return false;
            }

            // Uses Left channel of Timing Error audio file.
            // sample value range is -1.0 ≦ v < +1.0
            mTimingErrorFromAudioFile = ad.pcm[0].GetPcmInDouble(mTotalSamples);

            return true;
        }

#if true
        private double mSineArg;
        private int    mOffs;
        private double mSineJitterThetaCoefficient;

        /// <summary>
        /// Returns jitter added resample position
        /// retval unit is sample, difference from the ideal resample position of no jitter
        /// </summary>
        private double GenerateJitter2() {
            // 以下のsineJitterの計算、offsを掛けるのではなくて、前回の計算結果にcoeffを足してから
            // -π ≦ mSineArg < +π にする。

            double sineJitter = mSineJitterAmp * Math.Sin(mSineArg);
            double tpdfJitter = 0.0;
            double rpdfJitter = 0.0;
            double timingErrFromFile = 0.0;

            if (0.0 < mTpdfJitterAmp) {
                double r = GenRandom0to1(mRand) + GenRandom0to1(mRand) - 1.0;
                tpdfJitter = mTpdfJitterAmp * r;
            }
            if (0.0 < mRpdfJitterAmp) {
                rpdfJitter = mRpdfJitterAmp * (GenRandom0to1(mRand) * 2.0 - 1.0);
            }

            if (mTimingErrorFromAudioFile != null) {
                timingErrFromFile = 1.0e-9 * TimingErrorFileNanosec * mSampleRate * mTimingErrorFromAudioFile.At(mOffs);
            }

            double jitter = sineJitter + tpdfJitter + rpdfJitter + timingErrFromFile;

            mSineArg += mSineJitterThetaCoefficient;
            while (Math.PI < mSineArg) {
                mSineArg -= 2.0 * Math.PI;
            }

            ++mOffs;

            return jitter;
        }
#else
        /// <summary>
        /// Returns jitter added resample position
        /// retval unit is sample, difference from the ideal resample position of no jitter
        /// </summary>
        private double GenerateJitter(int offs) {
            double sineJitterThetaCoefficient = 2 * Math.PI * SineJitterFreq / mSampleRate;

            // 以下のsineJitterの計算、offsを掛けるのではなくて、前回の計算結果にcoeffを足してから %で
            // 剰余を計算する方が精度が良いかもしれない。両方作って比べると良いでしょう。
            double sineJitter = mSineJitterAmp
                * Math.Sin((sineJitterThetaCoefficient * offs) % (2.0 * Math.PI));
            double tpdfJitter = 0.0;
            double rpdfJitter = 0.0;
            double timingErrFromFile = 0.0;
            
            if (0.0 < mTpdfJitterAmp) {
                double r = GenRandom0to1(mRand) + GenRandom0to1(mRand) - 1.0;
                tpdfJitter = mTpdfJitterAmp * r;
            }
            if (0.0 < mRpdfJitterAmp) {
                rpdfJitter = mRpdfJitterAmp * (GenRandom0to1(mRand) * 2.0 - 1.0);
            }

            if (mTimingErrorFromAudioFile != null) {
                timingErrFromFile = 1.0e-9 * TimingErrorFileNanosec * mSampleRate
                    * mTimingErrorFromAudioFile.At(offs);
            }

            double jitter = sineJitter + tpdfJitter + rpdfJitter + timingErrFromFile;

            return jitter;
        }
#endif

        /// <summary>
        /// Setup mResamplePosArray and mFractionArray
        /// </summary>
        private void PrepareResamplePosArray(
                int sampleRate,
                int sampleTotal) {
            mResamplePosArray = new int[sampleTotal];
            mFractionArray    = new double[sampleTotal];
#if true
            mSineJitterThetaCoefficient = 2 * Math.PI * SineJitterFreq / mSampleRate;
            mSineArg = 0;
            mOffs = 0;
            for (int i = 0; i < sampleTotal; ++i) {
                double jitterVal = GenerateJitter2();

                // -0.5 <= jitterValF < +0.5になるようにjitterValFを選ぶ。
                int jitterValI = (int)(jitterVal + 0.5);
                double jitterValF = jitterVal - jitterValI;

                // 最後のほうで範囲外を指さないようにする。
                int resamplePosI = i + jitterValI;

                if (resamplePosI < 0) {
                    mResamplePosArray[i] = 0;
                    mFractionArray[i] = 0;
                } else if (sampleTotal <= resamplePosI) {
                    mResamplePosArray[i] = sampleTotal - 1;
                    mFractionArray[i] = 0;
                } else {
                    mResamplePosArray[i] = resamplePosI;
                    mFractionArray[i] = jitterValF;
                }
            }
#else
            // original algorithm
            for (int i = 0; i < sampleTotal; ++i) {
                // FIXME: ここでiが大きいときに値の精度がいくらか低下する!
                // resamplePosを経由せずに整数部と小数部を算出するように変えたら良い。
                double resamplePos = (double)i + GenerateJitter(i);

                // -0.5 <= fraction < +0.5になるようにresamplePosを選ぶ。
                // 最後のほうで範囲外を指さないようにする。
                int resamplePosI = (int)(resamplePos + 0.5);

                if (resamplePosI < 0) {
                    mResamplePosArray[i] = 0;
                    mFractionArray[i]    = 0;
                } else if (sampleTotal <= resamplePosI) {
                    mResamplePosArray[i] = sampleTotal - 1;
                    mFractionArray[i]    = 0;
                } else {
                    mResamplePosArray[i] = resamplePosI;
                    mFractionArray[i]    = resamplePos - resamplePosI;
                }
            }
#endif
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            if (Int32.MaxValue <= inputFormat.NumSamples) {
                MessageBox.Show("Input PCM data is too long");
                return null;
            }
            /*
                ジッター発生の原理
                ①ジッターによって揺さぶられたクロックで時を刻む。
                ②①の各時刻について、音声信号の波高値をSincリサンプラーにより算出する。

                sampleRate        == 96000 Hz
                jitterFrequency   == 50 Hz
                jitterPicoseconds == 1 ps の場合

                サンプル位置posのθ= 2 * PI * pos * 50 / 96000 (ラジアン)

                サンプル間隔= 1/96000秒 = 10.4 μs
             
                1ms = 10^-3秒
                1μs= 10^-6秒
                1ns = 10^-9秒
                1ps = 10^-12秒

                1psのずれ                     A サンプルのずれ
                ───────────── ＝ ─────────
                10.4 μs(1/96000)sのずれ      1 サンプルのずれ

                1psのサンプルずれA ＝ 10^-12 ÷ (1/96000) = 10^-12 * 96000 (サンプルのずれ)
             
                サンプルを採取する位置= pos + Asin(θ)
            */

            mTotalSamples = inputFormat.NumSamples;
            mSampleRate    = inputFormat.SampleRate;

            // SineJitterNanosecはRMS値で与えられる。振幅は√2倍。
            mSineJitterAmp = 1.0e-9 * SineJitterNanosec * inputFormat.SampleRate * Math.Sqrt(2);
            mTpdfJitterAmp = 1.0e-9 * TpdfJitterNanosec * inputFormat.SampleRate * 2;
            mRpdfJitterAmp = 1.0e-9 * RpdfJitterNanosec * inputFormat.SampleRate;

            if (!SetupTimingErrorFile()) {
                //MessageBox.Show("Error: JitterAddFilter::Setup() failed!");
                return null;
            }

            PrepareResamplePosArray(inputFormat.SampleRate, (int)inputFormat.NumSamples);
            return new PcmFormat(inputFormat);
        }

        public override long NumOfSamplesNeeded() {
            // inputデータを一度のFilterDo()で全て処理する(手抜き)。
            return mTotalSamples;
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();

            System.Diagnostics.Debug.Assert(inPcm.Length == mResamplePosArray.Length);
            System.Diagnostics.Debug.Assert(inPcm.Length == mFractionArray.Length);

            double [] outPcm = new double[inPcm.Length];

            Parallel.For(0, inPcm.Length, toPos => {
                int fromPos = mResamplePosArray[toPos];
                double fraction = mFractionArray[toPos];
                double sinFraction = Math.Sin(-Math.PI * mFractionArray[toPos]);
                double v = 0.0;

                for (int convOffs = -ConvolutionLengthMinus1/2; convOffs <= ConvolutionLengthMinus1/2; ++convOffs) {
                    long pos = convOffs + fromPos;
                    if (0 <= pos && pos < inPcm.LongLength) {
                        double x = Math.PI * (convOffs - fraction);

                        double sinX = sinFraction;
                        if (0 != (convOffs & 1)) {
                            sinX *= -1.0;
                        }

                        double sinc = SincD(sinX, x);

                        v += inPcm[pos] * sinc;
                    }
                }
                outPcm[toPos] = v;
            });

            return new WWUtil.LargeArray<double>(outPcm);
        }
    }
}

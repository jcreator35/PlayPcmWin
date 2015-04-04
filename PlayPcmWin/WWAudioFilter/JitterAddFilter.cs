using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;

namespace WWAudioFilter {
    public class JitterAddFilter : FilterBase {
        private RNGCryptoServiceProvider mRand = new RNGCryptoServiceProvider();

        // 入力パラメーター
        public double SineJitterFreq { get; set; }
        public double SineJitterNanosec { get; set; }
        public double TpdfJitterNanosec { get; set; }
        public double RpdfJitterNanosec { get; set; }
        public int ConvolutionLengthMinus1 { get; set; }

        // Setup()で計算する
        private long   mTotalSamples;
        private int    mSampleRate;
        private double mSineJitterAmp;
        private double mTpdfJitterAmp;
        private double mRpdfJitterAmp;

        // ジッターによって揺さぶられたクロックが生成した再サンプリング時刻。
        // PrepareResamplePosArray()で計算する
        private int[]    mResamplePosArray;
        private double[] mFractionArray;

        public JitterAddFilter(double sineJitterFreq, double sineJitterNanosec, double tpdfJitterNanosec, double rpdfJitterNanosec, int convolutionLengthMinus1)
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
        }

        public override FilterBase CreateCopy() {
            return new JitterAddFilter(SineJitterFreq, SineJitterNanosec, TpdfJitterNanosec, RpdfJitterNanosec, ConvolutionLengthMinus1);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterJitterAddDesc,
                SineJitterFreq, SineJitterNanosec, TpdfJitterNanosec, RpdfJitterNanosec, ConvolutionLengthMinus1+1);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4}",
                SineJitterFreq, SineJitterNanosec, TpdfJitterNanosec, RpdfJitterNanosec, ConvolutionLengthMinus1);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 6) {
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

            return new JitterAddFilter(sineJitterFreq, sineJitterNanosec, tpdfJitterNanosec, rpdfJitterNanosec, convolutionN);
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

        /// <summary>
        /// Returns jitter added resample position
        /// </summary>
        private double GenerateJitter(int offs) {
            double sineJitterThetaCoefficient = 2 * Math.PI * SineJitterFreq / mSampleRate;

            double sineJitter = mSineJitterAmp
                * Math.Sin((sineJitterThetaCoefficient * offs) % (2.0 * Math.PI));
            double tpdfJitter = 0.0;
            double rpdfJitter = 0.0;
            if (0.0 < mTpdfJitterAmp) {
                double r = GenRandom0to1(mRand) + GenRandom0to1(mRand) - 1.0;
                tpdfJitter = mTpdfJitterAmp * r;
            }
            if (0.0 < mRpdfJitterAmp) {
                rpdfJitter = mRpdfJitterAmp * (GenRandom0to1(mRand) * 2.0 - 1.0);
            }
            double jitter = sineJitter + tpdfJitter + rpdfJitter;
            return jitter;
        }

        /// <summary>
        /// Setup mResamplePosArray and mFractionArray
        /// </summary>
        private void PrepareResamplePosArray(
                int sampleRate,
                int sampleTotal) {
            mResamplePosArray = new int[sampleTotal];
            mFractionArray    = new double[sampleTotal];
            for (int i = 0; i < sampleTotal; ++i) {
                double resamplePos = (double)i + GenerateJitter(i); //< ここでiが大きいときに値の精度がいくらか低下する

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
            mSineJitterAmp = 1.0e-9 * SineJitterNanosec * inputFormat.SampleRate * Math.Sqrt(2);
            mTpdfJitterAmp = 1.0e-9 * TpdfJitterNanosec * inputFormat.SampleRate * 2;
            mRpdfJitterAmp = 1.0e-9 * RpdfJitterNanosec * inputFormat.SampleRate;

            PrepareResamplePosArray(inputFormat.SampleRate, (int)inputFormat.NumSamples);
            return new PcmFormat(inputFormat);
        }

        public override long NumOfSamplesNeeded() {
            return mTotalSamples;
        }

        public override double[] FilterDo(double[] inPcm) {
            System.Diagnostics.Debug.Assert(inPcm.Length == mResamplePosArray.Length);
            System.Diagnostics.Debug.Assert(inPcm.Length == mFractionArray.Length);

            double [] outPcm = new double[inPcm.LongLength];

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

            return outPcm;
        }
    }
}

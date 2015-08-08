using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace WWAudioFilter {
    class NoiseShapingFilter : FilterBase {
        public int TargetBitsPerSample { get; set; }
        public int NoiseShapingOrder { get; set; }
        private NoiseShaper2 mNoiseShaper;

        public NoiseShapingFilter(int targetBitsPerSample, int order)
                : base(FilterType.NoiseShaping) {
            if (targetBitsPerSample <= 0 || 24 < targetBitsPerSample) {
                throw new ArgumentOutOfRangeException("targetBitsPerSample");
            }
            TargetBitsPerSample = targetBitsPerSample;

            if (order != 2) {
                throw new ArgumentOutOfRangeException("order");
            }
            NoiseShapingOrder = order;
        }

        public override FilterBase CreateCopy() {
            return new NoiseShapingFilter(TargetBitsPerSample, NoiseShapingOrder);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterNoiseShapingDesc, NoiseShapingOrder, TargetBitsPerSample);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", TargetBitsPerSample, NoiseShapingOrder);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 3) {
                return null;
            }

            int tbps;
            if (!Int32.TryParse(tokens[1], out tbps) || tbps < 1 || 23 < tbps) {
                return null;
            }

            int order;
            if (!Int32.TryParse(tokens[2], out order) || order != 2) {
                return null;
            }

            return new NoiseShapingFilter(tbps, order);
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            return new PcmFormat(inputFormat);
        }

        public override void FilterStart() {
            base.FilterStart();
            mNoiseShaper = new NoiseShaper2(2, new double[] { 1, -2, 1 }, TargetBitsPerSample);
        }

        public override void FilterEnd() {
            base.FilterEnd();
            mNoiseShaper = null;
        }

        public override double[] FilterDo(double[] inPcm) {
            double [] outPcm = new double[inPcm.LongLength];
            
            for (long i=0; i < outPcm.LongLength; ++i) {
                double sampleD = inPcm[i];

                int sampleI24;
                if (1.0 <= sampleD) {
                    sampleI24 = 8388607;
                } else if (sampleD < -1.0) {
                    sampleI24 = -8388608;
                } else {
                    sampleI24 = (int)(8388608 * sampleD);
                }

                sampleI24 = mNoiseShaper.Filter24(sampleI24);

                outPcm[i] = (double)sampleI24 * (1.0 / 8388608);
            }

            return outPcm;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////////////////

        class NoiseShaper2 {
            private int  mOrder;
            private int  mQuantizedBit;
            private uint mMask;
            private double [] mDelay;
            private double [] mCoeffs;

            /// <summary>
            /// noise shaping filter
            /// </summary>
            /// <param name="order">filter order</param>
            /// <param name="coefficients">filter coefficients. element count == filter order+1</param>
            /// <param name="quantizedBit">target quantized bit. 1 to 23</param>
            public NoiseShaper2(int order, double[] coefficients, int quantizedBit) {
                if (order < 1) {
                    throw new System.ArgumentException();
                }
                mOrder = order;

                if (coefficients.Length != mOrder + 1) {
                    throw new System.ArgumentException();
                }
                mCoeffs = coefficients;

                mDelay = new double[mOrder];

                if (quantizedBit < 1 || 23 < quantizedBit) {
                    throw new System.ArgumentException();
                }
                mQuantizedBit = quantizedBit;
                mMask = 0xffffff00U << (24 - mQuantizedBit);
            }

            /// <summary>
            /// returns sample value its quantization bit is reduced to quantizedBit
            /// </summary>
            /// <param name="sampleFrom">input sample value. 24bit signed (-2^23 to +2^23-1)</param>
            /// <returns>noise shaping filter output sample. 24bit signed</returns>
            public int Filter24(int sampleFrom) {
                // convert quantized bit rate to 32bit
                sampleFrom <<= 8;

                double v = mCoeffs[0] * sampleFrom;

                for (int i=0; i < mOrder; ++i) {
                    v += mCoeffs[i + 1] * mDelay[i];
                }

                int sampleY;
                if (mQuantizedBit == 1) {
                    sampleY = (0 <= v) ? Int32.MaxValue : Int32.MinValue;
                } else {
                    if (v > Int32.MaxValue) {
                        v = Int32.MaxValue;
                    }
                    if (v < Int32.MinValue) {
                        v = Int32.MinValue;
                    }

                    sampleY = (int)(((int)v) & mMask);
                }
                // todo: コピーしないようにする
                for (int i=mOrder - 1; 0 < i; --i) {
                    mDelay[i] = mDelay[i - 1];
                }
                mDelay[0] = sampleY - v;

                return sampleY /= 256;
            }
        }

    }
}

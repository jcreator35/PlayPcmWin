// under construction

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace WWAudioFilter {
    class MashFilter : FilterBase {
        public int TargetBitsPerSample { get; set; }
        private NoiseShaperMash1bit mNoiseShaper1bit;

        public MashFilter(int targetBitsPerSample)
                : base(FilterType.Mash2) {
            if (targetBitsPerSample != 1) {
                throw new ArgumentOutOfRangeException("targetBitsPerSample");
            }
            TargetBitsPerSample = targetBitsPerSample;
        }

        public override FilterBase CreateCopy() {
            return new MashFilter(TargetBitsPerSample);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterMashDesc, TargetBitsPerSample);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", TargetBitsPerSample);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            int tbps;
            if (!Int32.TryParse(tokens[1], out tbps) || tbps != 1) {
                return null;
            }

            return new MashFilter(tbps);
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            return new PcmFormat(inputFormat);
        }

        public override void FilterStart() {
            base.FilterStart();

            mNoiseShaper1bit = new NoiseShaperMash1bit();
        }

        public override void FilterEnd() {
            base.FilterEnd();

            mNoiseShaper1bit = null;
        }

        public override double[] FilterDo(double[] inPcm) {
            double [] outPcm = new double[inPcm.LongLength];

            var dict = new Dictionary<int, int>();

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

                sampleI24 = mNoiseShaper1bit.Filter24(sampleI24);


                if (dict.ContainsKey(sampleI24)) {
                    ++dict[sampleI24];
                } else {
                    dict.Add(sampleI24,1);
                }

                outPcm[i] = (double)sampleI24 * (1.0 / 8388608);
            }

            foreach (var n in dict) {
                Console.WriteLine("{0} {1}", n.Key, n.Value);
            }

            return outPcm;
        }

        
        ///////////////////////////////////////////////////////////////////////
        // 1bit version

        class SigmaDelta1bit {
            private double mDelayX = Int32.MaxValue;
            private double mDelayY = Int32.MaxValue;
            private double mQuantizationError;

            /// <summary>
            /// SigmaDelta 1bit noise shaping system
            /// </summary>
            public SigmaDelta1bit() {
            }

            public double QuantizationError() {
                return mQuantizationError;
            }

            /// <summary>
            /// input sampleFrom, returns quantized sample value
            /// </summary>
            /// <param name="sampleFrom">input data. 24bit signed (-2^23 to +2^23-1)</param>
            /// <returns>filtered value. 24bit signed</returns>
            public int Filter24(double sampleFrom) {
                // convert quantized bit rate to 32bit integer
                sampleFrom *= 256;

                double x = sampleFrom + mDelayX - mDelayY;
                mDelayX = x;

                double y1q = x;

                int sampleY = (0 <= y1q) ? Int32.MaxValue : Int32.MinValue;
                mDelayY = sampleY;

                mQuantizationError = (sampleY - x) / 256;

                return sampleY / 256;
            }
        }

        class NoiseShaperMash1bit {
            private SigmaDelta1bit [] mSds;

            private SigmaDelta1bit mFinalQ;

            private double mDelayY2 = Int32.MaxValue / 256;

            public NoiseShaperMash1bit() {
                mSds = new SigmaDelta1bit[2];

                for (int i=0; i < 2; ++i) {
                    mSds[i] = new SigmaDelta1bit();
                }

                mFinalQ = new SigmaDelta1bit();
            }

            public int Filter24(int sampleFrom) {
                double y1 = mSds[0].Filter24(sampleFrom);
                double y2 = mSds[1].Filter24(mSds[0].QuantizationError());
                double rT = y1 + y2 - mDelayY2;
                mDelayY2 = y2;
                double r = mFinalQ.Filter24(rT / 4);
                return (int)r;
            }
        }
    }
}

using System;
using System.Globalization;
using System.Collections.Generic;

namespace WWAudioFilter {
    class CubicHermiteSplineUpsampler : FilterBase {
        public int Factor { get; set; }
        private List<double> mDelay;

        public CubicHermiteSplineUpsampler(int factor)
                : base(FilterType.CubicHermiteSplineUpsampler) {
            if (factor <= 1) {
                throw new ArgumentException("factor must be larger than 1");
            }

            Factor = factor;
        }

        public override FilterBase CreateCopy() {
            return new CubicHermiteSplineUpsampler(Factor);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterCubicHermiteSplineDesc, Factor);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", Factor);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            int factor;
            if (!Int32.TryParse(tokens[1], out factor) || factor <= 1) {
                return null;
            }

            return new CubicHermiteSplineUpsampler(factor);
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            var r = new PcmFormat(inputFormat);
            r.SampleRate *= Factor;
            r.NumSamples *= Factor;
            mDelay = new List<double>();
            return r;
        }

        // https://en.wikipedia.org/wiki/Cubic_Hermite_spline

        static double H00(double t) {
            return (1.0 + 2.0 * t) * (1.0 - t) * (1.0 - t);
        }

        static double H10(double t) {
            return t * (1.0 - t) * (1.0 - t);
        }

        static double H01(double t) {
            return t * t * (3.0 - 2.0 * t);
        }

        static double H11(double t) {
            return t * t * (t - 1.0);
        }


        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();

            int i = 0;
            double[] outPcm;
            if (mDelay.Count == 0) {
                outPcm = new double[(inPcm.Length - 3) * Factor];
                mDelay.Add(0);
                for (i = 0; i < 3; ++i) {
                    mDelay.Add(inPcm[i]);
                }
                // i==3
            } else {
                outPcm = new double[inPcm.Length * Factor];
                // i==0
            }

            int pos=0;
            for (; i < inPcm.Length; ++i) {
                // p0 and p1: 2つの隣接する入力サンプル値
                // m0 and m1: p0地点、p1地点の傾き
                double p0 = mDelay[1];
                double p1 = mDelay[2];
                double m0 = mDelay[2] - mDelay[0];
                double m1 = mDelay[3] - mDelay[1];

                for (int r=0; r < Factor; ++r) {
                    // tは補間時刻。0 < t ≦ 1まで変化する
                    double t = (r + 1.0) / Factor;
                    outPcm[pos] = H00(t) * p0 + H10(t) * m0 + H01(t) * p1 + H11(t) * m1;
                    ++pos;
                }

                mDelay.RemoveAt(0);
                mDelay.Add(inPcm[i]);
            }
            return new WWUtil.LargeArray<double>(outPcm);
        }
    }
}

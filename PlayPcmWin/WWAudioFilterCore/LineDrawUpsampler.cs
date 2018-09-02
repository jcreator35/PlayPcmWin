using System;
using System.Globalization;

namespace WWAudioFilterCore {
    public class LineDrawUpsampler : FilterBase {
        public int Factor { get; set; }
        private bool mFirst;

        private double mLastOriginalSampleValue;

        public LineDrawUpsampler(int factor)
                : base(FilterType.LineDrawUpsampler) {
            if (factor <= 1) {
                throw new ArgumentException("factor must be larger than 1");
            }

            Factor = factor;
        }

        public override FilterBase CreateCopy() {
            return new LineDrawUpsampler(Factor);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterLineDrawDesc, Factor);
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

            return new LineDrawUpsampler(factor);
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            var r = new PcmFormat(inputFormat);
            r.SampleRate *= Factor;
            r.NumSamples *= Factor;
            mFirst = true;
            return r;
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();

            double[] outPcm;
            int i = 0;

            if (mFirst) {
                outPcm = new double[(inPcm.Length - 1) * Factor];
                mLastOriginalSampleValue = inPcm[0];
                mFirst = false;
                i = 1;
            } else {
                outPcm = new double[inPcm.Length * Factor];
                // i==0
            }

            int pos = 0;
            for (; i < inPcm.Length; ++i) {
                for (int r = 0; r < Factor; ++r) {
                    double ratio = (r + 1.0) / Factor;
                    outPcm[pos] = inPcm[i] * ratio + mLastOriginalSampleValue * (1.0 - ratio);
                    ++pos;
                }
                mLastOriginalSampleValue = inPcm[i];
            }

            return new WWUtil.LargeArray<double>(outPcm);
        }
    }
}

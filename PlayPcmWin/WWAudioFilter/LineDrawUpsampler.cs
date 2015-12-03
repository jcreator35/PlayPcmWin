using System;
using System.Globalization;

namespace WWAudioFilter {
    class LineDrawUpsampler : FilterBase {
        public int Factor { get; set; }
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

        public override long NumOfSamplesNeeded() {
            return 8192;
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            var r = new PcmFormat(inputFormat);
            r.SampleRate *= Factor;
            r.NumSamples *= Factor;
            mLastOriginalSampleValue = 0.0;
            return r;
        }

        public override double[] FilterDo(double[] inPcm) {
            double [] outPcm = new double[inPcm.LongLength * Factor];
            long pos=0;
            for (long i=0; i < inPcm.LongLength; ++i) {

                for (int r=0; r < Factor; ++r) {
                    double ratio = (r + 1.0) / Factor;
                    outPcm[pos] = inPcm[i] * ratio + mLastOriginalSampleValue * (1.0 - ratio);
                    ++pos;
                }
                mLastOriginalSampleValue = inPcm[i];
            }
            return outPcm;
        }
    }
}

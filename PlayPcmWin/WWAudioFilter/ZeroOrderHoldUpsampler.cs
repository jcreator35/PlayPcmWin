using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace WWAudioFilter {
    class ZeroOrderHoldUpsampler : FilterBase {
        public int Factor { get; set; }

        public ZeroOrderHoldUpsampler(int factor)
                : base(FilterType.ZohUpsampler) {
            if (factor <= 1) {
                throw new ArgumentException("factor must be larger than 1");
            }

            Factor = factor;
        }

        public override FilterBase CreateCopy() {
            return new ZeroOrderHoldUpsampler(Factor);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterZOHDesc, Factor);
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

            return new ZeroOrderHoldUpsampler(factor);
        }

        public override long NumOfSamplesNeeded() {
            return 8192;
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            var r = new PcmFormat(inputFormat);
            r.SampleRate *= Factor;
            r.NumSamples *= Factor;
            return r;
        }

        public override PcmDataLib.LargeArray<double> FilterDo(PcmDataLib.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();
            var outPcm = new double[inPcm.LongLength * Factor];

            long pos=0;
            for (long i=0; i < inPcm.LongLength; ++i) {
                for (int r=0; r < Factor; ++r) {
                    outPcm[pos] = inPcm[i];
                    ++pos;
                }
            }

            return new PcmDataLib.LargeArray<double>(outPcm);
        }
    }
}

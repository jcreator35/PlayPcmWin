using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace WWAudioFilterCore {
    public class InsertZeroesUpsampler : FilterBase {
        public int Factor { get; set; }
        private const int BATCH_PROCESS_SAMPLES = 4096;

        public InsertZeroesUpsampler(int factor)
                : base(FilterType.InsertZeroesUpsampler) {
            if (factor <= 1) {
                throw new ArgumentException("factor must be larger than 1");
            }

            Factor = factor;
        }

        public override FilterBase CreateCopy() {
            return new InsertZeroesUpsampler(Factor);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterInsertZeroesDesc, Factor);
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

            return new InsertZeroesUpsampler(factor);
        }

        public override long NumOfSamplesNeeded() {
            return BATCH_PROCESS_SAMPLES;
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            var r = new PcmFormat(inputFormat);
            r.SampleRate *= Factor;
            r.NumSamples *= Factor;
            return r;
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            System.Diagnostics.Debug.Assert(inPcmLA.LongLength == NumOfSamplesNeeded());

            var inPcm = inPcmLA.ToArray();

            var outPcm = new double[inPcm.Length * Factor];
            for (int i = 0; i < inPcm.Length; ++i) {
                outPcm[i*Factor] = inPcm[i];
            }

            return new WWUtil.LargeArray<double>(outPcm);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WWAudioFilterCore {
    public class Downsampler : FilterBase {
        public int Factor { get; set; }
        public int PickSampleIndex { get; set; }

        public Downsampler(int factor, int pickSampleIndex)
                : base(FilterType.Downsampler) {
            if (factor <= 1) {
                throw new ArgumentException("factor must be larger than 1");
            }
            Factor = factor;

            if (pickSampleIndex < 0 || factor <= pickSampleIndex) {
                throw new ArgumentException("pickSampleIndex out of range");
            }
            PickSampleIndex = pickSampleIndex;
        }

        public override FilterBase CreateCopy() {
            return new Downsampler(Factor, PickSampleIndex);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterDownsamplerDesc, Factor, PickSampleIndex+1);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", Factor, PickSampleIndex);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 3) {
                return null;
            }

            int factor;
            if (!Int32.TryParse(tokens[1], out factor) || factor <= 1) {
                return null;
            }

            int pickSampleIndex;
            if (!Int32.TryParse(tokens[2], out pickSampleIndex) || pickSampleIndex < 0 || factor <= pickSampleIndex) {
                return null;
            }

            return new Downsampler(factor, pickSampleIndex);
        }

        public override long NumOfSamplesNeeded() {
            return 1024 * Factor;
        }

        public override void FilterStart() {
            base.FilterStart();
        }

        public override void FilterEnd() {
            base.FilterEnd();
        }
        
        public override PcmFormat Setup(PcmFormat inputFormat) {
            var r = new PcmFormat(inputFormat);
            r.SampleRate /= Factor;
            r.NumSamples /= Factor;
            return r;
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();
            System.Diagnostics.Debug.Assert(inPcm.Length == NumOfSamplesNeeded());

            var outPcm = new double[inPcm.Length / Factor];

            long writePos = 0;
            for (long i = PickSampleIndex; i < inPcm.Length; i+=Factor) {
                outPcm[writePos] = inPcm[i];
                ++writePos;
            }

            return new WWUtil.LargeArray<double>(outPcm);
        }
    }
}

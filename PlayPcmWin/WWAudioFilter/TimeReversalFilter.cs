using System;
using System.Globalization;

namespace WWAudioFilter {
    public class TimeReversalFilter : FilterBase {

        public TimeReversalFilter()
            : base(FilterType.TimeReversal) {
        }

        public override FilterBase CreateCopy() {
            return new TimeReversalFilter();
        }

        public override string ToDescriptionText() {
            return Properties.Resources.FilterTimeReversalDesc;
        }

        public override string ToSaveText() {
            return String.Empty;
        }

        public static FilterBase Restore(string[] tokens) {
            return new TimeReversalFilter();
        }

        PcmFormat mFormat;

        public override PcmFormat Setup(PcmFormat inputFormat) {
            mFormat = inputFormat;
            return inputFormat;
        }

        public override long NumOfSamplesNeeded() {
            return mFormat.NumSamples;
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcm) {
            WWUtil.LargeArray<double> outPcm = new WWUtil.LargeArray<double>(inPcm.LongLength);
            for (long i=0; i < outPcm.LongLength; ++i) {
                outPcm.Set(i, inPcm.At(inPcm.LongLength - i - 1));
            }
            return outPcm;
        }
    }
}

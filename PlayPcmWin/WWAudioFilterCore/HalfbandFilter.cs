using System;
using System.Collections.Generic;
using System.Globalization;

namespace WWAudioFilterCore {
    public class HalfbandFilter : FilterBase {
        private const int BATCH_PROCESS_SAMPLES = 4096;
        private readonly int FILTER_DELAY;

        public int FilterLength { get; set; }

        private Queue<double> mSampleDelay = new Queue<double>();
        private double[] mFilterCoeffs;

        public HalfbandFilter(int filterLength)
            : base(FilterType.HalfbandFilter) {

            if (3 != (filterLength & 0x3)) {
                throw new ArgumentException("filterLength +1 must be multiply of 4");
            }
            FilterLength = filterLength;

            FILTER_DELAY = (FilterLength + 1) / 2;
        }

        public override FilterBase CreateCopy() {
            return new HalfbandFilter(FilterLength);
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            DesignFilter();
            return inputFormat;
        }

        public override void FilterStart() {
            base.FilterStart();

            mSampleDelay.Clear();
            for (int i = 0; i < FilterLength - FILTER_DELAY; ++i) {
                mSampleDelay.Enqueue(0);
            }
        }

        public override void FilterEnd() {
            base.FilterEnd();
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterHalfbandDesc, FilterLength);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", FilterLength);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            int filterLength;
            if (!Int32.TryParse(tokens[1], out filterLength) || filterLength <= 0 || 3 != (filterLength & 3)) {
                return null;
            }

            return new HalfbandFilter(filterLength);
        }

        public override long NumOfSamplesNeeded() {
            return BATCH_PROCESS_SAMPLES;
        }

        private void DesignFilter() {
            mFilterCoeffs = new double[FilterLength];

            var sine90Table = new double[] { 0.0, 1.0, 0.0, -1.0 };

            for (int i = 0; i < FILTER_DELAY; ++i) {
                if (i != 0 && 0 == (i & 1)) {
                    // coefficient is 0
                    continue;
                }
                double theta = Math.PI * (i * 90.0) / 180.0f;
                double v = 1.0;
                if (Double.Epsilon < Math.Abs(theta)) {
                    v = sine90Table[i & 3] / theta;
                }
                mFilterCoeffs[FILTER_DELAY - 1 - i] = v;
                mFilterCoeffs[FILTER_DELAY - 1 + i] = v;
            }

            // Kaiser窓(α==9)をかける
            var w = WWWindowFunc.KaiserWindow(FilterLength, 9.0);
            for (int i = 0; i < FilterLength; ++i) {
                mFilterCoeffs[i] *= w[i];
            }

            // 0.5倍する
            for (int i = 0; i < FilterLength; ++i) {
                mFilterCoeffs[i] *= 0.5;
            }

        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();

            var result = new double[inPcm.Length - (mFilterCoeffs.Length - mSampleDelay.Count - 1)];
            int pos = 0;
            foreach (double v in inPcm) {
                mSampleDelay.Enqueue(v);

                if (mFilterCoeffs.Length <= mSampleDelay.Count) {
                    double sum = 0.0;
                    int offs = 0;
                    foreach (double d in mSampleDelay) {
                        sum += mFilterCoeffs[offs] * d;
                        ++offs;
                    }
                    result[pos] = sum;
                    ++pos;

                    mSampleDelay.Dequeue();
                }
            }

            return new WWUtil.LargeArray<double>(result);
        }
    }
}

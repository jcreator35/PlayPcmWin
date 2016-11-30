/* Linear-phase DC removal filter based on
 * Richard G. Lyons, Understanding Digital Signal Processing 3rd ed. Pearson
 * p814 Figure 13-100
 */

using System;
using System.Globalization;
using System.Collections.Generic;

namespace WWAudioFilter {
    public class SubsonicFilter : FilterBase {
        public double CutoffFreq { get; set; }
        private int mSampleRate;

        private int mDelaySamples;
        private List<MovingAverager> mMovingAveragerList;
        private Delay mDelayX;
        private int mDiscardSamples;

        private const int MOVING_AVERAGER_NUM = 4;

        public SubsonicFilter(double cutoffFreq)
            : base(FilterType.SubsonicFilter) {
            CutoffFreq = cutoffFreq;
        }

        public override FilterBase CreateCopy() {
            return new SubsonicFilter(CutoffFreq);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.SubsonicFilterDesc,
                CutoffFreq);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", CutoffFreq);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            double k;
            if (!Double.TryParse(tokens[1], out k)) {
                return null;
            }

            return new SubsonicFilter(k);
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            mSampleRate = inputFormat.SampleRate;
            mDelaySamples = (int)(mSampleRate / CutoffFreq / 2 / 1.2);

            mMovingAveragerList = new List<MovingAverager>();
            for (int i = 0; i < MOVING_AVERAGER_NUM; ++i) {
                mMovingAveragerList.Add(new MovingAverager(mDelaySamples));
            }
            mDelayX = new Delay(mDelaySamples*2 - 2);
            mDiscardSamples = mDelaySamples * 2 - 2;
            return inputFormat;
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();
            var outPcm = new double[inPcm.Length];

            for (int i = 0; i < inPcm.Length; ++i) {
                double x = inPcm[i];

                double delayedY = mDelayX.Filter(x);

                for (int j = 0; j < MOVING_AVERAGER_NUM; ++j) {
                    x = mMovingAveragerList[j].Filter(x);
                }

                double y = delayedY - x;
                outPcm[i] = y;
            }

            if (0 < mDiscardSamples) {
                if (outPcm.Length < mDiscardSamples) {
                    mDiscardSamples -= outPcm.Length;
                    return new WWUtil.LargeArray<double>(0);
                } else {
                    var outPcm2 = new double[outPcm.Length - mDiscardSamples];
                    Array.Copy(outPcm, mDiscardSamples, outPcm2, 0, outPcm2.Length);

                    mDiscardSamples = 0;
                    return new WWUtil.LargeArray<double>(outPcm2);
                }
            }

            return new WWUtil.LargeArray<double>(outPcm);
        }
    }

}

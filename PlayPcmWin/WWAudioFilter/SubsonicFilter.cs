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
            mDelaySamples = (int)(mSampleRate / CutoffFreq);

            mMovingAveragerList = new List<MovingAverager>();
            for (int i = 0; i < MOVING_AVERAGER_NUM; ++i) {
                mMovingAveragerList.Add(new MovingAverager(mDelaySamples));
            }
            mDelayX = new Delay(mDelaySamples*2 - 2);
            return inputFormat;
        }

        public override double[] FilterDo(double[] inPcm) {
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

            return outPcm;
        }
    }

}

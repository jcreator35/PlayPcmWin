using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace WWAudioFilterCore {
    public class ReduceBitDepth : FilterBase {
        public int TargetBitsPerSample { get; set; }

        public ReduceBitDepth(int targetBitsPerSample)
                : base(FilterType.ReduceBitDepth) {
            if (targetBitsPerSample <= 0 || 24 < targetBitsPerSample) {
                throw new ArgumentOutOfRangeException("targetBitsPerSample");
            }
            TargetBitsPerSample = targetBitsPerSample;
        }

        public override FilterBase CreateCopy() {
            return new ReduceBitDepth(TargetBitsPerSample);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterReduceBitDepthDesc, TargetBitsPerSample);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", TargetBitsPerSample);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            int tbps;
            if (!Int32.TryParse(tokens[1], out tbps) || tbps < 1 || 23 < tbps) {
                return null;
            }

            return new ReduceBitDepth(tbps);
        }

        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();

            var outPcm = new double[inPcm.Length];

            // 32bit integerにサンプル値を左寄せで詰める。
            // maskで下位ビットを0にすることで量子化ビット数を減らす。

            uint mask = 0xffffff00U << (24 - TargetBitsPerSample);

            for (long i = 0; i < outPcm.Length; ++i) {
                double sampleD = inPcm[i];

                int sampleI24;
                if (1.0 <= sampleD) {
                    sampleI24 = 8388607;
                } else if (sampleD < -1.0) {
                    sampleI24 = -8388608;
                } else {
                    sampleI24 = (int)(8388608 * sampleD);
                }

                int sampleI32 = sampleI24 << 8;

                if (TargetBitsPerSample == 1) {
                    sampleI32 = (0 <= sampleI32) ? Int32.MaxValue : Int32.MinValue;
                } else {
                    sampleI32 = (int)(((int)sampleI32) & mask);
                }

                sampleI24 = sampleI32 >> 8;

                outPcm[i] = (double)sampleI24 * (1.0 / 8388608);
            }

            return new WWUtil.LargeArray<double>(outPcm);
        }

    }
}

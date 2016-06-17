using System;
using System.Globalization;

namespace WWAudioFilter {
    class NoiseShaping4thFilter : FilterBase {
        public int TargetBitsPerSample { get; set; }

        public NoiseShaping4thFilter(int targetBitsPerSample)
            : base(FilterType.NoiseShaping4th) {
            if (targetBitsPerSample < 1 || 23 < targetBitsPerSample) {
                throw new ArgumentOutOfRangeException("targetBitsPerSample");
            }
            TargetBitsPerSample = targetBitsPerSample;
        }

        public override FilterBase CreateCopy() {
            return new NoiseShaping4thFilter(TargetBitsPerSample);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterNoiseShaping4thDesc, TargetBitsPerSample);
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

            return new NoiseShaping4thFilter(tbps);
        }

        public override PcmFormat Setup(PcmFormat inputFormat) {
            mMask = 0xffffff00U << (24 - TargetBitsPerSample);
            return new PcmFormat(inputFormat);
        }

        double [] mS = new double [5];

        /* Following filter coefficients are from
          Derk Reefman and Erwin Janssen, "One-Bit Audio: An Overview" J. Audio Eng. Soc., Vol 52, No. 3 pp. 187 (2004)
         */
        static readonly double [] mC = new double [] { 0.791882, 0.304545, 0.069930, 0.009496, 0.000607 };
        static readonly double [] mF = new double [] { 0.000496, 0.001789 };

        private uint mMask;

        public override void FilterStart() {
            base.FilterStart();
            Array.Clear(mS, 0, mS.Length);
        }

        public override void FilterEnd() {
            base.FilterEnd();
            Array.Clear(mS, 0, mS.Length);
        }

        public override PcmDataLib.LargeArray<double> FilterDo(PcmDataLib.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();

            var outPcm = new double[inPcm.Length];

            for (int i=0; i < outPcm.Length; ++i) {
                double sampleD = 0.0;
                for (int order=0; order < 5; ++order) {
                    sampleD += mC[order] * mS[order];
                }

                if (1 == TargetBitsPerSample) {
                    if (0.0 <= sampleD) {
                        outPcm[i] = 8388607.0 / 8388608.0;
                    } else {
                        outPcm[i] = -1.0;
                    }
                } else {
                    int sampleI = 0;
                    if (1.0f <= sampleD) {
                        sampleI = Int32.MaxValue;
                    } else if (sampleD < -1.0f) {
                        sampleI = Int32.MinValue;
                    } else {
                        sampleI = (int)(sampleD * -1.0 * Int32.MinValue);
                    }

                    sampleI = (int)((sampleI) & mMask);

                    outPcm[i] = sampleI * (-1.0 / Int32.MinValue);
                }

                mS[4] += mS[3];
                mS[3] += mS[2] - mF[1] * mS[4];
                mS[2] += mS[1];
                mS[1] += mS[0] - mF[0] * mS[2];
                mS[0] += (inPcm[i] - outPcm[i]);
            }

            return new PcmDataLib.LargeArray<double>(outPcm);
        }

    }
}

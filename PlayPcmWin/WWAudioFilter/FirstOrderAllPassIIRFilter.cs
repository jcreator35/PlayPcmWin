using System;
using System.Globalization;

namespace WWAudioFilter {
    public class FirstOrderAllPassIIRFilter : FilterBase {
        public double K { get; set; }
        private double mLastX;
        private double mLastY;

        public FirstOrderAllPassIIRFilter(double k)
                : base(FilterType.FirstOrderAllPassIIR) {
            K = k;
        }

        public override FilterBase CreateCopy() {
            return new FirstOrderAllPassIIRFilter(K);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterFirstOrderAllPassIIRDesc,
                K);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", K);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 2) {
                return null;
            }

            double k;
            if (!Double.TryParse(tokens[1], out k)) {
                return null;
            }

            return new FirstOrderAllPassIIRFilter(k);
        }

        public override void FilterStart() {
            mLastX = 0;
            mLastY = 0;
            base.FilterStart();
        }

        public override void FilterEnd() {
            base.FilterEnd();
        }

        /*
         * Transfer function H(z):
         *             k + z^{-1}
         *   H(z) = ------------------
         *            1 + k * z^{-1}
         *
         * Differential equation:
         * Input:  x[n]
         * Output: y[n]
         * 
         * y[n] = k * x[n] + x[n-1] - k * y[n-1]
         */
        public override double[] FilterDo(double[] inPcm) {
            var outPcm = new double[inPcm.Length];

            for (int i=0; i < inPcm.Length; ++i) {
                double x = inPcm[i];

                // direct form implementation of the difference equation
                double y = K * x + mLastX;
                y += -K * mLastY;

                outPcm[i] = y;

                mLastX = x;
                mLastY = y;

                //Console.WriteLine("K={0:g} n={1:g} y={2:g}", K, i, y);
            }

            return outPcm;
        }
    }
}

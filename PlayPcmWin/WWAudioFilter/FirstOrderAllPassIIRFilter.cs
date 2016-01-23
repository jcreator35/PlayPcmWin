using System;
using System.Globalization;

namespace WWAudioFilter {
    public class FirstOrderAllPassIIRFilter : FilterBase {
        public double A { get; set; }
        private double mLastX;
        private double mLastY;

        public FirstOrderAllPassIIRFilter(double a)
                : base(FilterType.FirstOrderAllPassIIR) {
            A = a;
        }

        public override FilterBase CreateCopy() {
            return new FirstOrderAllPassIIRFilter(A);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterFirstOrderAllPassIIRDesc,
                A);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0}", A);
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
         *            -a + z^{-1}
         *   H(z) = ------------------
         *            1 - a * z^{-1}
         * 
         *       for a : real value
         * 
         * Difference equation:
         * Input:  x[n]
         * Output: y[n]
         * 
         * y[n] = -a * x[n] + x[n-1] + a * y[n-1]
         */
        public override double[] FilterDo(double[] inPcm) {
            var outPcm = new double[inPcm.Length];

            for (int i=0; i < inPcm.Length; ++i) {
                double x = inPcm[i];

                // direct form implementation of the difference equation
                double y = -A * x + mLastX;
                y += A * mLastY;

                outPcm[i] = y;

                mLastX = x;
                mLastY = y;

                //Console.WriteLine("A={0:g} n={1:g} y={2:g}", A, i, y);
            }

            return outPcm;
        }
    }
}

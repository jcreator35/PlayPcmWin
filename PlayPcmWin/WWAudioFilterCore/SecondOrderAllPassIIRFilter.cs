using System;
using System.Globalization;

namespace WWAudioFilterCore {
    public class SecondOrderAllPassIIRFilter : FilterBase {
        public double R { get; set; }

        /// <summary>
        /// degrees
        /// </summary>
        public double T { get; set; }
        private double[] mLastX = new double[2];
        private double[] mLastY = new double[2];

        public SecondOrderAllPassIIRFilter(double r, double tDegrees)
                : base(FilterType.SecondOrderAllPassIIR) {
            R = r;
            T = tDegrees;
        }

        public override FilterBase CreateCopy() {
            return new SecondOrderAllPassIIRFilter(R, T);
        }

        public override string ToDescriptionText() {
            return string.Format(CultureInfo.CurrentCulture, Properties.Resources.FilterSecondOrderAllPassIIRDesc,
                R, T);
        }

        public override string ToSaveText() {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", R, T);
        }

        public static FilterBase Restore(string[] tokens) {
            if (tokens.Length != 3) {
                return null;
            }

            double r;
            if (!Double.TryParse(tokens[1], out r)) {
                return null;
            }
            double t;
            if (!Double.TryParse(tokens[2], out t)) {
                return null;
            }

            return new SecondOrderAllPassIIRFilter(r, t);
        }

        public override void FilterStart() {
            for (int i = 0; i < 1; ++i) {
                mLastX[i] = 0;
                mLastY[i] = 0;
            }
            base.FilterStart();
        }

        public override void FilterEnd() {
            base.FilterEnd();
        }

        /*
         * Transfer function H(z):
         *           (r・e^{jθ} - z^{-1})(r・e^{-jθ} - z^{-1})
         *   H(z) = ------------------------------------------------
         *           (1 - r・e^{jθ}・z^{-1})(1 - r・e^{-jθ}・z^{-1})
         *
         *           r^2 - 2r・cos(θ)z^{-1} + z^{-2}
         *        = -------------------------------
         *           1 - 2r・cos(θ)z^{-1} + r^2・z^{-2}
         *           
         * Difference equation:
         * Input:  x[n]
         * Output: y[n]
         * 
         * y[n] = r^2 * x[n] - 2rcosθ * x[n-1] + x[n-2] + 2rcosθ * y[n-1] - r^2 * y[n-2]
         */
        public override WWUtil.LargeArray<double> FilterDo(WWUtil.LargeArray<double> inPcmLA) {
            var inPcm = inPcmLA.ToArray();
            var outPcm = new double[inPcm.Length];

            double θ = T * Math.PI / 180.0;

            for (int i=0; i < inPcm.Length; ++i) {
                double x = inPcm[i];

                // direct form implementation of the difference equation
                double y = R * R * x - 2 * R * Math.Cos(θ) * mLastX[0] + mLastX[1];
                y += 2 * R * Math.Cos(θ) * mLastY[0] - R * R * mLastY[1];

                outPcm[i] = y;

                mLastX[1] = mLastX[0];
                mLastX[0] = x;
                mLastY[1] = mLastY[0];
                mLastY[0] = y;
            }

            return new WWUtil.LargeArray<double>(outPcm);
        }
    }
}

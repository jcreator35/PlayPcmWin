// 日本語

using System;

namespace WWAudioFilterCore {
    /// <summary>
    /// Direct-form 2 Biquad filter
    /// </summary>
    public class BiquadFilterD2 {
        public double[] c;
        public double[] z;
        /// <summary>
        /// Transposed Direct-form 2 Biquad filter
        /// </summary>
        /// <param name="aCoeffs">[b0 b1 b2 a0 a1 a2]</param>
        public BiquadFilterD2(double[] aCoeffs) {
            if (aCoeffs.Length != 6) {
                throw new ArgumentOutOfRangeException("aCoeffs");
            }
            c = aCoeffs;
            z = new double[2];
        }

        public void Reset() {
            z[0] = 0;
            z[1] = 0;
        }

        public double Filter(double x) {
            double b0 = c[0];
            double b1 = c[1];
            double b2 = c[2];
            double a0 = c[3];
            double a1 = c[4];
            double a2 = c[5];

            double z0 = z[0];
            double z1 = z[1];

            double w1 = z0;
            double w2 = z1;
            double w0 = x - a1 * w1 - a2 * w2;
            double y = b0 * w0 + b1 * w1 + b2 * w2;

            z[0] = w0;
            z[1] = w1;

            return y;
        }
    }
}

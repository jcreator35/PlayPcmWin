// 日本語

using System;

namespace WWAudioFilterCore {
    /// <summary>
    /// Transposed Direct-form 2 Biquad filter
    /// </summary>
    public class BiquadFilter2T {
        public double[] c;
        public double[] z;
        /// <summary>
        /// Transposed Direct-form 2 Biquad filter
        /// </summary>
        /// <param name="aCoeffs">[b1 b2 b3 a1 a2 a3]</param>
        public BiquadFilter2T(double[] aCoeffs) {
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

            double y = x * b0 + z0;
            z[0] = x * b1 + z1 - a1 * y;
            z[1] = x * b2 - a2 * y;

            return y;
        }
    }
}

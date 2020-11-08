using System;
using WWMath;

namespace DesignFrequencySamplingFIRFilter {
    class CalcFIRFilterFrequencyResponse {
        /// <summary>
        /// FIRフィルターの各周波数ωにおけるゲインを戻す。
        /// </summary>
        /// <param name="filterCoeffs">FIRフィルター係数。</param>
        /// <param name="ω">各周波数。ラジアン。</param>
        /// <returns>ゲイン(複素数)。</returns>
        public WWComplex Calc(double[] filterCoeffs, double ω) {
            double real=0;
            double imag=0;

            for (int k = 0; k < filterCoeffs.Length; ++k) {
                real += filterCoeffs[k] * Math.Cos(ω * k);
                imag += -filterCoeffs[k] * Math.Sin(ω * k);
            }

            return new WWComplex(real, imag);
        }
    }
}

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

            for (int i = 0; i < filterCoeffs.Length; ++i) {
                real += filterCoeffs[i] * Math.Cos(ω * i);
                imag += -filterCoeffs[i] * Math.Sin(ω * i);
            }

            return new WWComplex(real, imag);
        }
    }
}

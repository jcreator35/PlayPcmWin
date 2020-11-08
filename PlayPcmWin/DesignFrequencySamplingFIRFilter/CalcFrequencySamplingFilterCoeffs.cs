using System;

namespace DesignFrequencySamplingFIRFilter {
    class CalcFrequencySamplingFilterCoeffs {

        /// <summary>
        /// FIRフィルターの係数計算。
        /// Reference: J.G. Proakis & D.G. Manolakis: Digital Signal Processing, 4th edition, 2007, Chapter 10, pp. 671-678
        /// Mは奇数、α=0
        /// </summary>
        /// <param name="Hr">k個(k==(M+1)/2個)の等間隔の角周波数ω_k=2πk/M, k=0,1,2,...,(M-1)/2におけるフィルターゲイン値Hr(ω_k) == Hr(2πk/M)。</param>
        /// <returns>FIRフィルター係数h(n)。全てのFIRフィルター係数が出てくる。</returns>
        public double [] Calc(int M, double [] Hr) {
            if (1 != (M & 1)) {
                throw new ArgumentException("This program accepts only odd M");
            }
            if (Hr.Length != (int)((M+1)/2)) {
                throw new ArgumentException("Hr count should be (M+1)/2");
            }

            // create G(k) : G[k]
            var G = new double[Hr.Length];
            for (int k=0; k<G.Length; ++k) {
                G[k] = Math.Pow(-1.0, k) * Hr[k];
            }

            // create h(n) : hn[n]
            var hn = new double[M];
            for (int n=0; n<G.Length; ++n) {
                double h = (1.0 / M) * G[0];
                for (int k = 1; k < G.Length; ++k) {
                    h += (2.0 / M) * G[k] * Math.Cos(2.0 * Math.PI * k * (n + 1.0 / 2.0) / M);
                }
                hn[n] = h;
            }

            // h(n) is symmetry.
            for (int n=0; n<G.Length-1; ++n) {
                hn[hn.Length - n-1] = hn[n];
            }

            return hn;
        }
    }
}

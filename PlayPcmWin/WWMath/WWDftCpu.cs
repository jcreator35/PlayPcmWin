using System;

namespace WWMath {
    public class WWDftCpu {
        /// <summary>
        /// 1次元DFT。要素数Nとして、N分の1した結果を戻す。
        /// </summary>
        /// <param name="from">入力。</param>
        /// <param name="to">出力DFT結果</param>
        public static void Dft1d(WWComplex[] from, out WWComplex[] to) {
            /*
             * W=e^(j*2pi/N)
             * Gp = (1/N) * seriesSum(Sk * W^(k*p), k, 0, N-1) (p=0,1,2,…,(N-1))
             * 
             * from == Sk
             * to   == Gp
             */

            // 要素数N
            int n = from.Length;
            double recipN = 1.0 / n;

            to = new WWComplex[n];

            // Wのテーブル
            var w = new WWComplex[n];
            for (int i=0; i < n; ++i) {
                double re = Math.Cos(-i * 2.0 * Math.PI / n);
                double im = Math.Sin(-i * 2.0 * Math.PI / n);
                w[i] = new WWComplex(re, im);
            }

            for (int p=0; p < n; ++p) {
                double gr = 0.0;
                double gi = 0.0;
                for (int k=0; k < n; ++k) {
                    int posSr = k;
                    int posWr = ((p * k) % n);
                    double sR = from[posSr].real;
                    double sI = from[posSr].imaginary;
                    double wR = w[posWr].real;
                    double wI = w[posWr].imaginary;
                    gr += sR * wR - sI * wI;
                    gi += sR * wI + sI * wR;
                }
                double re = gr * recipN;
                double im = gi * recipN;
                to[p] = new WWComplex(re, im);
            }
        }

        /// <summary>
        /// 1次元IDFT。要素数で割ったりはしない。Dft1dとペアで使用すると値が元に戻る。
        /// </summary>
        /// <param name="from">入力</param>
        /// <param name="to">出力DFT結果</param>
        public static void Idft1d(WWComplex[] from, out WWComplex[] to) {
            /*
             * W=e^(-j*2pi/N)
             * Sk= seriesSum([ Gp * W^(-k*p) ], p, 0, N-1) (k=0,1,2,…,(N-1))
             * 
             * from == Gp
             * to   == Sk
             */

            // 要素数N
            int n = from.Length;

            to = new WWComplex[n];

            // Wのテーブル
            var w = new WWComplex[n];
            for (int i=0; i < n; ++i) {
                double re = Math.Cos(i * 2.0 * Math.PI / n);
                double im = Math.Sin(i * 2.0 * Math.PI / n);
                w[i] = new WWComplex(re, im);
            }

            // IDFT実行。
            for (int k=0; k < n; ++k) {
                double sr = 0.0;
                double si = 0.0;
                for (int p=0; p < n; ++p) {
                    int posGr = p;
                    int posWr = (p * k) % n;
                    double gR = from[posGr].real;
                    double gI = from[posGr].imaginary;
                    double wR = w[posWr].real;
                    double wI = w[posWr].imaginary;
                    sr += gR * wR - gI * wI;
                    si += gR * wI + gI * wR;
                }
                to[k] = new WWComplex(sr, si);
            }
        }
    }
}

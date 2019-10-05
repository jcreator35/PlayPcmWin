// 日本語。

using System;

namespace WWMath {
    public class WWDftCpu {
        /// <summary>
        /// 1次元DFT。要素数N。compensationを出力値に乗算する。省略時1/N。Idft1dとペアで使用すると値が元に戻る。
        /// </summary>
        /// <param name="from">入力。</param>
        /// <param name="compensation">出力に乗算する係数。省略時1/N</param>
        /// <returns>出力DFT結果</returns>
        public static WWComplex[] Dft1d(WWComplex[] from, double? compensation = null) {
            /*
             * W=e^(j*2pi/N)
             * Gp = (1/N) * seriesSum(Sk * W^(k*p), k, 0, N-1) (p=0,1,2,…,(N-1))
             * 
             * from == Sk
             * to   == Gp
             */

            // 要素数N
            int N = from.Length;

            var to = new WWComplex[N];

            // Wのテーブル
            var w = new WWComplex[N];
            for (int i=0; i < N; ++i) {
                double re = Math.Cos(-i * 2.0 * Math.PI / N);
                double im = Math.Sin(-i * 2.0 * Math.PI / N);
                w[i] = new WWComplex(re, im);
            }

            for (int p=0; p < N; ++p) {
                double gr = 0.0;
                double gi = 0.0;
                for (int k=0; k < N; ++k) {
                    int posSr = k;
                    int posWr = ((p * k) % N);
                    double sR = from[posSr].real;
                    double sI = from[posSr].imaginary;
                    double wR = w[posWr].real;
                    double wI = w[posWr].imaginary;
                    gr += sR * wR - sI * wI;
                    gi += sR * wI + sI * wR;
                }
                to[p] = new WWComplex(gr, gi);
            }

            double c = 1.0 / N;
            if (compensation != null) {
                c = (double)compensation;
            }

            if (c != 1.0) {
                var toC = new WWComplex[N];
                for (int i = 0; i < N; ++i) {
                    toC[i] = new WWComplex(to[i].real * c, to[i].imaginary * c);
                }

                return toC;
            } else {
                return to;
            }
        }

        /// <summary>
        /// 1次元DFT。要素数N。compensationを出力値に乗算する。省略時1/N。Idft1dとペアで使用すると値が元に戻る。
        /// </summary>
        /// <param name="from">入力。</param>
        /// <param name="to">出力DFT結果</param>
        /// <param name="compensation">出力に乗算する係数。省略時1/N</param>
        public static void Dft1d(WWComplex[] from, out WWComplex[] to, double? compensation = null) {
            var r = Dft1d(from, compensation);
            to = r;
        }

        /// <summary>
        /// 1次元IDFT。compensationを出力値に乗算する。Dft1dとペアで使用すると値が元に戻る。
        /// </summary>
        /// <param name="from">入力</param>
        /// <param name="compensation">出力に乗算する係数。省略時1</param>
        /// <returns>出力IDFT結果</returns>
        public static WWComplex[] Idft1d(WWComplex[] from, double? compensation = null) {
            /*
             * W=e^(-j*2pi/N)
             * Sk= seriesSum([ Gp * W^(-k*p) ], p, 0, N-1) (k=0,1,2,…,(N-1))
             * 
             * from == Gp
             * to   == Sk
             */

            // 要素数N
            int N = from.Length;

            var to = new WWComplex[N];

            // Wのテーブル
            var w = new WWComplex[N];
            for (int i=0; i < N; ++i) {
                double re = Math.Cos(i * 2.0 * Math.PI / N);
                double im = Math.Sin(i * 2.0 * Math.PI / N);
                w[i] = new WWComplex(re, im);
            }

            // IDFT実行。
            for (int k=0; k < N; ++k) {
                double sr = 0.0;
                double si = 0.0;
                for (int p=0; p < N; ++p) {
                    int posGr = p;
                    int posWr = (p * k) % N;
                    double gR = from[posGr].real;
                    double gI = from[posGr].imaginary;
                    double wR = w[posWr].real;
                    double wI = w[posWr].imaginary;
                    sr += gR * wR - gI * wI;
                    si += gR * wI + gI * wR;
                }
                to[k] = new WWComplex(sr, si);
            }

            double c = 1.0;
            if (compensation != null) {
                c = (double)compensation;
            }

            if (c != 1.0) {
                var toC = new WWComplex[N];
                for (int i = 0; i < N; ++i) {
                    toC[i] = new WWComplex(to[i].real * c, to[i].imaginary * c);
                }

                return toC;
            } else {
                return to;
            }
        }

        /// <summary>
        /// 1次元IDFT。compensationを出力値に乗算する。Dft1dとペアで使用すると値が元に戻る。
        /// </summary>
        /// <param name="from">入力</param>
        /// <param name="compensation">出力に乗算する係数。省略時1</param>
        /// <param name="to">出力IDFT結果</param>
        public static void Idft1d(WWComplex[] from, out WWComplex[] to, double? compensation = null) {
            var r = Idft1d(from, compensation);
            to = r;
        }
    }
}

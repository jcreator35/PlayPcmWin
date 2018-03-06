using System;

namespace WWDirectComputeCS {

    /// <summary>
    /// 高速フーリエ変換。
    /// C♯で書かれており、特に最適化も行っていないので、すごく高速ではない。
    /// introduction to algorithms second edition p.840
    /// </summary>
    public class WWFFTCpu {
        /// <summary>
        /// いわゆるビットリバース
        /// 最適化とかはしてない平凡な実装。
        /// </summary>
        /// <param name="v">リバースしたい値</param>
        /// <param name="lgLen">lg(値の総数)</param>
        /// <returns>bit-reversed value</returns>
        static int ReverseBit(int v, int lgLen) {
            int rv = 0;
            for (int i=0; i<lgLen; ++i) {
                rv += (v & (1<<i)) != 0 ? (1<<(lgLen-i-1)) : 0;
            }
            return rv;
        }

        /// <summary>
        /// 底2のlog(v)。vは整数。
        /// vが1のとき 0を戻す
        /// vが2のとき 1を戻す
        /// vが4のとき 2を戻す
        /// vが8のとき 3を戻す
        /// </summary>
        /// <returns>vが2の乗数ならば、0以上の値。vが2の乗数でないとき-1</returns>
        static int Lg(int v) {
            if (v <= 0) {
                return -1;
            }

            int rv = 0;
            int acc = 0;
            for (int i=30; 0 <= i; --i) {
                if (((v >> i) & 1) == 1) {
                    rv = i;
                    ++acc;
                }
            }

            if (acc != 1) {
                return -1;
            }
            return rv;
        }

        /// <summary>
        /// 複素数がre0, im0, re1, im1, …の順で入っているfromの要素をビットリバース順に並び替えた配列を出力する。
        /// 最適化とかはしていない。
        /// </summary>
        static double[] BitReverseCopyComplex(double[] from) {
            int lgNum = Lg(from.Length/2);
            System.Diagnostics.Debug.Assert(0 < lgNum);

            var rv = new double[from.Length];
            for (int i=0; i < from.Length/2; ++i) {
                int rev = ReverseBit(i, lgNum);
                rv[rev * 2+0] = from[i * 2+0]; // real part
                rv[rev * 2+1] = from[i * 2+1]; // imaginary part
            }

            return rv;
        }

        /// <summary>
        /// 2のべき乗
        /// v=0 のとき 戻り値 1
        /// v=1 のとき 戻り値 2
        /// v=2 のとき 戻り値 4
        /// v=3 のとき 戻り値 8
        /// v=30のとき 戻り値 2^30
        /// </summary>
        static int Pow2(int v) {
            System.Diagnostics.Debug.Assert(0 <= v && v < 31);
            return 1<<v;
        }

        /// <summary>
        /// 複素数FFTする
        /// introduction to algorithms second edition p.841
        /// </summary>
        /// <param name="from">複素数がre0, im0, re1, im1, …の順で入っている</param>
        /// <returns>複素数がre0, im0, re1, im1, …の順で入っている</returns>
        public static double[] ComplexFFT(double[] from) {
            var a = BitReverseCopyComplex(from);

            // 複素数の要素数n
            int n = from.Length/2;

            // 底2のlog(要素数)
            int lgNum = Lg(n);
            
            for (int s=1; s <= lgNum; ++s) {
                var m = Pow2(s);
                // System.Console.WriteLine("s={0} m={1} n={2}", s, m, n);
                for (int k=0; k < n; k += m) {
                    for (int j=0; j < m / 2; ++j) {

                        // butterfly operation

                        // 注: introduction to algorithmsは、omegaはガウス平面で反時計回りだが、
                        // WWDFTに合わせて時計回りにする
                        var theta = -2.0 * Math.PI * j / m;
                        var omegaMRe = Math.Cos(theta);
                        var omegaMIm = Math.Sin(theta);

                        // System.Console.WriteLine("k={0} j={1} k+j={2} k+j+m/2={3}", k, j, k + j, k + j + m / 2);

                        int posUx2 = 2 * (k + j);
                        int posTx2 = 2 * (k + j + m / 2);

                        var uRe = a[posUx2 + 0];
                        var uIm = a[posUx2 + 1];
                        var tRe = omegaMRe * a[posTx2 + 0] - omegaMIm * a[posTx2 + 1];
                        var tIm = omegaMRe * a[posTx2 + 1] + omegaMIm * a[posTx2 + 0];

                        a[posUx2 + 0] = uRe + tRe;
                        a[posUx2 + 1] = uIm + tIm;
                        a[posTx2 + 0] = uRe - tRe;
                        a[posTx2 + 1] = uIm - tIm;
                    }
                }
            }

            // 要素数nで割る
            for (int i=0; i < a.Length; ++i) {
                a[i] /= n;
            }

            return a;
        }

        /// <summary>
        /// 複素数IFFTする
        /// </summary>
        /// <param name="from">複素数がre0, im0, re1, im1, …の順で入っている</param>
        /// <returns>複素数がre0, im0, re1, im1, …の順で入っている</returns>
        public static double[] ComplexIFFT(double[] from) {
            var a = BitReverseCopyComplex(from);

            // 複素数の要素数n
            int n = from.Length / 2;

            // 底2のlog(要素数)
            int lgNum = Lg(n);

            for (int s=1; s <= lgNum; ++s) {
                var m = Pow2(s);
                // System.Console.WriteLine("s={0} m={1} n={2}", s, m, n);
                for (int k=0; k < n; k += m) {
                    for (int j=0; j < m / 2; ++j) {

                        // butterfly operation

                        // FFTと逆回転に回せば良い
                        var theta = 2.0 * Math.PI * j / m;
                        var omegaMRe = Math.Cos(theta);
                        var omegaMIm = Math.Sin(theta);

                        // System.Console.WriteLine("k={0} j={1} k+j={2} k+j+m/2={3}", k, j, k + j, k + j + m / 2);

                        int posUx2 = 2 * (k + j);
                        int posTx2 = 2 * (k + j + m / 2);

                        var uRe = a[posUx2 + 0];
                        var uIm = a[posUx2 + 1];
                        var tRe = omegaMRe * a[posTx2 + 0] - omegaMIm * a[posTx2 + 1];
                        var tIm = omegaMRe * a[posTx2 + 1] + omegaMIm * a[posTx2 + 0];

                        a[posUx2 + 0] = uRe + tRe;
                        a[posUx2 + 1] = uIm + tIm;
                        a[posTx2 + 0] = uRe - tRe;
                        a[posTx2 + 1] = uIm - tIm;
                    }
                }
            }

            return a;
        }
    }
}

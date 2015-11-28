using System;

namespace WWAudioFilter {
    /// <summary>
    /// 窓関数置き場。
    /// </summary>
    sealed public class WWWindowFunc {
        private WWWindowFunc() {
        }

        /// <summary>
        /// ブラックマン窓
        /// </summary>
        /// <param name="window">[out]窓Wk 左右対称の形状が出てくる。奇数である必要あり。</param>
        /// <returns>窓の長さn(nは奇数) 要素番号(length-1)/2が山のピーク</returns>
        public static double [] BlackmanWindow(int length) {
            // nは奇数
            System.Diagnostics.Debug.Assert((length & 1) == 1);

            var window = new double[length];

            // 教科書通りに計算すると両端の値が0.0になって
            // せっかくのデータが0にされて勿体無いので両端(pos==0とpos==length-1)の値はカットし、両端を1ずつ広げる
            int m = length + 1;
            for (int i=0; i < length; ++i) {
                int pos = i + 1;
                double v = 0.42 - 0.5 * Math.Cos(2.0 * Math.PI * pos / m) + 0.08 * Math.Cos(4.0 * Math.PI * pos / m);
                window[i] = v;
            }

            return window;
        }

        /// <summary>
        /// 0以上の整数値vの階乗
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        private static long Factorial(int v) {
            System.Diagnostics.Debug.Assert(0 <= v);

            // vが21以上でlongがオーバーフローする
            System.Diagnostics.Debug.Assert(v <= 20);

            if (v <= 1) {
                return 1;
            }

            long rv = 1;
            for (int i=2; i <= v; ++i) {
                rv *= i;
            }
            return rv;
        }

        /// <summary>
        /// 0次の第1種変形ベッセル関数I0(alpha)
        /// </summary>
        /// <param name="alpha">引数</param>
        /// <returns>I0(alpha)</returns>
        private static double ModifiedBesselI0(double alpha) {
            const int L=15;

            double i0 = 1.0;
            for (int l=1; l < L; ++l) {
                double t = Math.Pow(alpha * 0.5, l) / Factorial(l);
                i0 += t * t;
            }
            return i0;
        }

        /// <summary>
        /// カイザー窓
        /// </summary>
        /// <param name="length">窓の長さn(nは奇数) 要素番号(length-1)/2が山のピーク</param>
        /// <param name="alpha">Kaiser窓のパラメータα</param>
        /// <returns>窓Wk 左右対称の形状が出てくる。</returns>
        public static double [] KaiserWindow(int length, double alpha) {
            // αは4より大きく9より小さい
            System.Diagnostics.Debug.Assert(4 <= alpha && alpha <= 9);

            // カイザー窓は両端の値が0にならないので普通に計算する。
            var window = new double[length];
            int m = length-1;
            for (int i=0; i < length; ++i) {
                int pos = i;

                // 分母i0d
                double i0d = ModifiedBesselI0(alpha);

                // 分子i0n
                double t2 = (1.0 - 2.0 * pos / m);
                double a = alpha * Math.Sqrt(1.0 - t2 * t2);
                double i0n = ModifiedBesselI0(a);

                window[i] = i0n / i0d;
            }

            return window;
        }
    }
}

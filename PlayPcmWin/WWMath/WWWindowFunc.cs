using System;
using WWUtil;

namespace WWMath {
    /// <summary>
    /// 窓関数置き場。
    /// </summary>
    sealed public class WWWindowFunc {
        private WWWindowFunc() {
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // 周波数ドメインの窓関数。
        // 使用例はWWSlidingDFT.FilterWithWindow()。

        public enum WindowType {
            Hamming,
            Hanning,
            Blackman,
            BH3,
        };

        /// <summary>
        /// 時間ドメインで乗算する代わりに、
        /// 周波数ドメインでコンボリューションするときに使う窓関数の係数。
        /// 使用例はWWSlidingDFT.FilterWithWindow()。
        /// </summary>
        public static double[] FreqDomainWindowCoeffs(WindowType wt) {
            // Richard G. Lyons, Understanding Digital Signal Processing, 3 rd Ed., Pearson, 2011, pp. 686

            switch (wt) {
            case WindowType.Hamming:
                return new double[] { 0.54, 0.46 };
            case WindowType.Hanning:
                return new double[] { 0.5, 0.5 };
            case WindowType.Blackman:
                return new double[] { 7938.0 / 18608, 9240.0 / 18608, 1430.0 / 18608 };
            case WindowType.BH3:
                return new double[] { 0.42323, 0.49755, 0.07922 };
            default:
                System.Diagnostics.Debug.Assert(false);
                return null;
            }
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■
        // 時間ドメインの窓関数。

        /// <summary>
        /// Bartlett窓(三角形)
        /// Time dependent Fourier transformに使用すると入力値が完全に復元できるという特徴あり。
        /// </summary>
        /// <returns>窓の長さn(nは奇数) 要素番号(length-1)/2が山のピーク。両端の値は0(教科書通り)。</returns>
        /// <returns>Bartlett窓 最大値1</returns>
        public static double[] BartlettWindow(int length) {
            // nは奇数
            System.Diagnostics.Debug.Assert((length & 1) == 1);

            int t = length / 2;

            var w = new double[length];
            for (int i = 0; i < length / 2+1; ++i) {
                w[i] = (double)i / t;
                w[w.Length - 1 - i] = (double)i / t;
            }

            return w;
        }

        /// <summary>
        /// Hann (Hanning) 窓
        /// コサイン関数+DC。
        /// Time dependent Fourier transformに使用すると入力値が完全に復元できるという特徴あり。
        /// </summary>
        /// <returns>窓の長さn(nは奇数) 要素番号(length-1)/2が山のピーク。両端の値は0(教科書通り)。</returns>
        /// <returns>Hann (Hanning)窓 最大値1</returns>
        public static double[] HannWindow(int length) {
            // nは奇数
            System.Diagnostics.Debug.Assert((length & 1) == 1);

            var w = new double[length];
            for (int i = 0; i < length; ++i) {
                double θ = 2.0 * Math.PI * ((double)i / (length-1));

                double x = 0.5 - 0.5 * Math.Cos(θ);
                w[i] = x;
            }

            return w;
        }

        /// <summary>
        /// ブラックマン窓
        /// </summary>
        /// <param name="length">窓の長さn(nは奇数) 要素番号(length-1)/2が山のピーク。両端の値は0でないので注意。</param>
        /// <returns>[out]窓Wk 左右対称の形状が出てくる。奇数である必要あり。</returns>
        public static double[] BlackmanWindow(int length) {
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

        public static LargeArray<double> BlackmanWindow(long length) {
            // nは奇数
            System.Diagnostics.Debug.Assert((length & 1) == 1);

            var window = new LargeArray<double>(length);

            // 教科書通りに計算すると両端の値が0.0になって
            // せっかくのデータが0にされて勿体無いので両端(pos==0とpos==length-1)の値はカットし、両端を1ずつ広げる
            long m = length + 1;
            for (long i = 0; i < length; ++i) {
                long pos = i + 1;
                double v = 0.42 - 0.5 * Math.Cos(2.0 * Math.PI * pos / m) + 0.08 * Math.Cos(4.0 * Math.PI * pos / m);
                window.Set(i, v);
            }

            return window;
        }

        /// <summary>
        /// 0以上の整数値vの階乗
        /// </summary>
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
        /// 0次の第1種変形ベッセル関数I_0(alpha)
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
        /// <param name="length">窓の長さn(nは奇数) 要素番号(length-1)/2が山のピーク。両端の値は0でない。</param>
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

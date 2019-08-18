using System;

namespace WWDigitalFilter {
    public class LoopFilterCRFB {
        private readonly double[] mA;
        private readonly double[] mB;
        private readonly double[] mG;
        private double[] mZ;

        private readonly int mOrder;

        public int Order {
            get { return mOrder; }
        }

        public LoopFilterCRFB(double[] aCoeffs, double[] bCoeffs, double [] gCoeffs) {
            mA = aCoeffs;
            mB = bCoeffs;
            mG = gCoeffs;

            mZ = new double[aCoeffs.Length];
            
            mOrder = aCoeffs.Length;

            if (mB.Length != mA.Length + 1) {
                throw new ArgumentOutOfRangeException();
            }
        }

        private void Reset() {
            for (int i = 0; i < mZ.Length; ++i) {
                mZ[i] = 0;
            }
        }

        /// <summary>
        /// ループフィルターに1個データを入力し、1個データを出力する。
        /// </summary>
        /// <param name="u">入力値x。</param>
        /// <returns>出力値y。+1か-1が出る。</returns>
        public int Filter(double u) {
            int odd = (mOrder & 1) == 1 ? 1 : 0;

            // CRFB構造。
            // R. Schreier and G. Temes, ΔΣ型アナログ/デジタル変換器入門,丸善,2007, pp.97

            // 最終出力vを計算する。
            double y = mZ[mOrder-1] + mB[mB.Length - 1] * u;
            int v = (0 <= y) ? 1 : -1; //< 1ビット量子化処理。

            if (odd == 1) {
                // 奇数次のCRFB。

                for (int i = mOrder - 2; 1 <= i; i -= 2) {
                    // 無遅延積分器のディレイmZ[i]
                    mZ[i    ] += mZ[i - 1] + mB[i    ] * u - mA[i    ] * v - mG[i / 2] * mZ[i + 1];
                    // 遅延積分器のディレイmZ[i+1]
                    mZ[i + 1] += mZ[i    ] + mB[i + 1] * u - mA[i + 1] * v;
                }

                // 奇数次の時最初に遅延積分器がある。mZ[0]の値を更新する。
                mZ[0] += mB[0] * u - mA[0] * v;
            } else {
                // 偶数次のCRFB。

                for (int i = mOrder - 2; 2 <= i; i -= 2) {
                    // 無遅延積分器のディレイmZ[i]
                    mZ[i] += mZ[i - 1] + mB[i] * u - mA[i] * v - mG[i / 2] * mZ[i + 1];
                    // 遅延積分器のディレイmZ[i+1]
                    mZ[i + 1] += mZ[i] + mB[i + 1] * u - mA[i + 1] * v;
                }

                // 0番の共振器は1個前の共振器からの入力mZ[-1]が無い。
                // 無遅延積分器のディレイmZ[0]
                mZ[0] += mB[0] * u - mA[0] * v - mG[0] * mZ[1];
                // 遅延積分器のディレイmZ[1]
                mZ[1] += mZ[0] + mB[1] * u - mA[1] * v;
            }

            return v;
        }

        /// <summary>
        /// ループフィルターに1個データを入力し、1個データを出力する。
        /// </summary>
        /// <param name="x">入力値x (資料ではu)。</param>
        /// <param name="quantizedValue">量子化後出力値 +1か-1。</param>
        /// <returns>コスト値w^2。</returns>
        public double Filter(double x, int quantizedValue) {
            int odd = (mOrder & 1) == 1 ? 1 : 0;

            // CRFB構造。
            // R. Schreier and G. Temes, ΔΣ型アナログ/デジタル変換器入門,丸善,2007, pp.97

            // フィルター出力y (連続値)を計算する。
            double y = mZ[mOrder - 1] + mB[mB.Length - 1] * x;
            int v = quantizedValue;

            if (odd == 1) {
                // 奇数次のCRFB。

                for (int i = mOrder - 2; 1 <= i; i -= 2) {
                    // 無遅延積分器のディレイmZ[i]
                    mZ[i] += mZ[i - 1] + mB[i] * x - mA[i] * v - mG[i / 2] * mZ[i + 1];
                    // 遅延積分器のディレイmZ[i+1]
                    mZ[i + 1] += mZ[i] + mB[i + 1] * x - mA[i + 1] * v;
                }

                // 奇数次の時最初に遅延積分器がある。mZ[0]の値を更新する。
                mZ[0] += mB[0] * x - mA[0] * v;
            } else {
                // 偶数次のCRFB。

                for (int i = mOrder - 2; 2 <= i; i -= 2) {
                    // 無遅延積分器のディレイmZ[i]
                    mZ[i] += mZ[i - 1] + mB[i] * x - mA[i] * v - mG[i / 2] * mZ[i + 1];
                    // 遅延積分器のディレイmZ[i+1]
                    mZ[i + 1] += mZ[i] + mB[i + 1] * x - mA[i + 1] * v;
                }

                // 0番の共振器は1個前の共振器からの入力mZ[-1]が無い。
                // 無遅延積分器のディレイmZ[0]
                mZ[0] += mB[0] * x - mA[0] * v - mG[0] * mZ[1];
                // 遅延積分器のディレイmZ[1]
                mZ[1] += mZ[0] + mB[1] * x - mA[1] * v;
            }

            return (y-v)*(y-v);
        }
    }
}

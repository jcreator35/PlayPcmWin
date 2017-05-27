
using System;
namespace WWOfflineResampler {
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
        /// <param name="x">入力値x。</param>
        /// <returns>出力値y。+1か-1が出る。</returns>
        public int Filter(double x) {
            var X = new double[Order];
            int odd = (Order & 1) == 1 ? 1 : 0;

            // CRFB構造。
            // R. Schreier and G. Temes, ΔΣ型アナログ/デジタル変換器入門,丸善,2007, pp.97

            // 最終出力mY
            X[X.Length - 1] = mZ[mZ.Length - 1];
            double v = X[X.Length - 1] + mB[mB.Length - 1] * x;
            int y = (0 <= v) ? 1 : -1;

            for (int i = mA.Length-2; odd <= i; i -= 2) {
                X[i+1] = mZ[i+1];
                X[i] = mZ[i] + -mG[i / 2] * X[i + 1] + mB[i] * x - mA[i] * y;

                mZ[i] = X[i];
                mZ[i + 1] += X[i] + mB[i+1] * x - mA[i+1] * y;
            }

            if (odd == 1) {
                // 奇数次の時最初に遅延積分器がある。
                X[0] = mZ[0];

                mZ[0] += mB[0] * x - mA[0] * y;
            }

            X = null;

            return y;
        }
    }
}

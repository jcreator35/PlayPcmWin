
using System;
namespace WWOfflineResampler {
    public class LoopFilterCRFB {
        private double[] mA;
        private double[] mB;
        private double[] mG;
        private double[] mZ;
        private double[] mX;
        private int mY;

        public LoopFilterCRFB(double[] aCoeffs, double[] bCoeffs, double [] gCoeffs) {
            mA = aCoeffs;
            mB = bCoeffs;
            mG = gCoeffs;

            mZ = new double[aCoeffs.Length];
            mX = new double[aCoeffs.Length];

            if (mB.Length != mA.Length + 1) {
                throw new ArgumentOutOfRangeException();
            }
        }

        private void Reset() {
            for (int i = 0; i < mZ.Length; ++i) {
                mZ[i] = 0;
            }
            mY = 0;
        }

        /// <summary>
        /// ループフィルターに1個データを入力し、1個データを出力する。
        /// </summary>
        /// <param name="x">入力値x。</param>
        /// <returns>出力値y。+1か-1が出る。</returns>
        public int Filter(double x) {
            // CRFB構造。B[len]==1
            // R. Schreier and G. Temes, ΔΣ型アナログ/デジタル変換器入門,丸善,2007, pp.97

            // 最終出力
            mX[mX.Length - 1] = mZ[mZ.Length - 1];
            double vR = mX[mX.Length - 1] + mB[mB.Length - 1] * x;
            mY = (0 <= vR) ? 1 : -1;

            for (int i = mA.Length-2; 0<=i; i-=2) {
                mX[i+1] = mZ[i+1];
                mX[i] = mZ[i] + -mG[i / 2] * mX[i + 1] + mB[i] * x - mA[i] * mY;

                mZ[i] = mX[i];
                mZ[i + 1] += mX[i] + mB[i+1] * x - mA[i+1] * mY;
            }

            return mY;
        }
    }
}

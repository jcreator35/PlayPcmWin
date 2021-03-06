﻿// 日本語

// Richard G. Lyons, Understanding Digital Signal Processing, 3 rd Ed., Pearson, 2011, pp. 786

using System;

namespace WWMath {
    public class WWQuadratureOscillatorInt {
        int mFt;
        int mFs;
        long mLcm;
        long mCounter;

        double mYi;
        double mYq;

        double mθ;
        double mCosθ;
        double mSinθ;

        /// <summary>
        /// Quadrature Oscillator, int frequency version
        /// </summary>
        /// <param name="ft">Oscillator turning freq。-fs ＜ ft ＜ fs</param>
        /// <param name="fs">Sample freq。0より大きい値</param>
        public WWQuadratureOscillatorInt(int ft, int fs) {
            // fsは正の値。
            if (fs <= 0) {
                throw new ArgumentOutOfRangeException("fs");
            }

            // -fs < ft < fs
            if (fs <= ft) {
                throw new ArgumentOutOfRangeException("ft");
            }
            if (ft <= -fs) {
                throw new ArgumentOutOfRangeException("ft");
            }

            mFt = ft;
            mFs = fs;

            if (mFt == 0) {
                mLcm = 1;
            } else if (mFt < 0) {
                // 定期的に位相をリセットします。
                mLcm = Functions.LCM(-ft, fs);
            } else {
                // 定期的に位相をリセットします。
                mLcm = Functions.LCM(ft, fs);
            }

            mθ = 2.0 * Math.PI * mFt / mFs;
            mCosθ = Math.Cos(mθ);
            mSinθ = Math.Sin(mθ);

            Reset();
        }

        public void Reset() {
            mYi = 1.0;
            mYq = 0.0;
            mCounter = mLcm -1;
        }

        public WWComplex Next() {
            if (mFt == 0) {
                return WWComplex.Unity();
            }

            // mFt != 0の場合。

            ++mCounter;
            if (mCounter == mLcm) {
                mLcm = 0;

                // 位相を0にリセットします。
                double gn = 3.0 / 2.0 - 0.5 * ((mYi * mYi) + (mYq * mYq));
                double yi = gn;
                double yq = 0;

                mYi = yi;
                mYq = yq;

                return new WWComplex(yi, yq);
            } else {
                double gn = 3.0 / 2.0 - 0.5 * ((mYi * mYi) + (mYq * mYq));
                double yi = gn * (mYi * mCosθ - mYq * mSinθ);
                double yq = gn * (mYq * mCosθ + mYi * mSinθ);

                mYi = yi;
                mYq = yq;

                return new WWComplex(yi, yq);
            }
        }
    }
}

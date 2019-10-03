// 日本語

// Richard G. Lyons, Understanding Digital Signal Processing, 3 rd Ed., Pearson, 2011, pp. 786

using System;

namespace WWMath {
    public class WWQuadratureOscillator {
        double mFt;
        double mFs;

        double mYi;
        double mYq;

        double mθ;
        double mCosθ;
        double mSinθ;

        /// <summary>
        /// Quadrature Oscillator
        /// </summary>
        /// <param name="ft">Oscillator turning freq</param>
        /// <param name="fs">Sample freq</param>
        public WWQuadratureOscillator(double ft, double fs) {
            mFt = ft;
            mFs = fs;

            mθ = 2.0 * Math.PI * mFt / mFs;
            mCosθ = Math.Cos(mθ);
            mSinθ = Math.Sin(mθ);

            Reset();
        }

        public void Reset() {
            mYi = 1.0;
            mYq = 0.0;
        }

        public WWComplex Next() {
            double gn = 3.0 / 2.0 - ((mYi * mYi) + (mYq * mYq));
            double yi = gn * (mYi * mCosθ - mYq * mSinθ);
            double yq = gn * (mYq * mCosθ - mYi * mSinθ);

            mYi = yi;
            mYq = yq;

            return new WWComplex(yi, yq);
        }

    }
}

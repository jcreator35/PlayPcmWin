using WWMath;
using System;

namespace WWIIRFilterDesign {
    /// <summary>
    /// Building block of IIR filter. used by IIRFilter class
    /// </summary>
    class IIRFilterBlockReal {
        private RealRationalPolynomial mH;
        /// <summary>
        /// フィードバックの係数
        /// </summary>
        private double[] mA;

        /// <summary>
        /// フィードフォワードの係数
        /// </summary>
        private double[] mB;

        /// <summary>
        /// ディレイ
        /// </summary>
        private double[] mV;

        private int mMaxOrder;
        
        public override string ToString() {
            return mH.ToString();
        }

        public IIRFilterBlockReal(RealRationalPolynomial p) {
            mH = p;
            mMaxOrder = p.NumerDegree();
            if (mMaxOrder < p.DenomDegree()) {
                mMaxOrder = p.DenomDegree();
            }
            
            mV = new double[mMaxOrder + 1];

            mB = new double[mMaxOrder + 1];
            for (int i = 0; i <= p.NumerDegree(); ++i) {
                mB[i] = p.N(i);
            }

            mA = new double[mMaxOrder + 1];
            mA[0] = p.D(0);
            for (int i = 1; i < mA.Length; ++i) {
                mA[i] = -p.D(i);
            }

            // mA[0]が 1.0になるようにスケールする
            for (int i = 0; i <= p.NumerDegree(); ++i) {
                mB[i] /= mA[0];
            }
            // mA[0]が最後まで残るように逆順でループする
            for (int i = mA.Length - 1; 0 <= i; --i) {
                mA[i] /= mA[0];
            }
        }

        public double Filter(double x) {
            double y = 0;

            switch (mMaxOrder) {
            case 2:
                // Transposed Direct form 2 structure
                // Discrete-time signal processing 3rd edition pp.427 figure 6.26 and equation 6.44a-d

                // equation 6.44a and 6.44b
                y = mB[0] * x + mV[1];

                // equation 6.44c
                mV[1] = mA[1] * y + mB[1] * x + mV[2];
                mV[2] = mA[2] * y + mB[2] * x;
                break;
            case 1:
                // equation 6.44a and 6.44b
                y = mB[0] * x + mV[1];

                // equation 6.44c
                mV[1] = mA[1] * y + mB[1] * x;
                break;
            default:
                throw new NotImplementedException();
            }

            return y;

        }
    }
}

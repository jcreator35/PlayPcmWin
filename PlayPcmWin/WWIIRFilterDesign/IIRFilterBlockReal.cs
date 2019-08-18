using WWMath;
using System;

namespace WWIIRFilterDesign {
    /// <summary>
    /// Building block of IIR filter. used by IIRFilter class
    /// </summary>
    public class IIRFilterBlockReal {
        private RealRationalPolynomial mH;

        private int mMaxOrder;

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

        private IIRFilterBlockReal() { }

        /// <summary>
        /// 同じフィルター特性、ディレイの状態も同じだが、別実体のディレイを持つインスタンスを作る。
        /// </summary>
        /// <returns></returns>
        public IIRFilterBlockReal CreateCopy() {
            var r = new IIRFilterBlockReal();
            r.mH = mH;
            r.mMaxOrder = mMaxOrder;
            r.mA = mA;
            r.mB = mB;

            r.mV = new double[mMaxOrder + 1];
            Array.Copy(mV, r.mV, mV.Length);

            return r;
        }

        public double[] A() { return mA; }
        public double[] B() { return mB; }

        public override string ToString() {
            return mH.ToString();
        }

        private bool AlmostEquals(double a, double b) {
            return ( Math.Abs(a - b) < 1e-8 );
        }

        public IIRFilterBlockReal(RealRationalPolynomial p) {
            // 分母の定数項が1.0になるようにスケールする
            p = p.ScaleAllCoeffs(1.0f / p.D(0));

            mH = p;

            mMaxOrder = p.NumerDegree();
            if (mMaxOrder < p.DenomDegree()) {
                mMaxOrder = p.DenomDegree();
            }
            
            mB = new double[mMaxOrder + 1];
            for (int i = 0; i <= p.NumerDegree(); ++i) {
                mB[i] = p.N(i);
            }

            mA = new double[mMaxOrder + 1];
            mA[0] = p.D(0);
            for (int i = 1; i < mA.Length; ++i) {
                mA[i] = -p.D(i);
            }

            mV = new double[mMaxOrder + 1];
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

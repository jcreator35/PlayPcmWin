using WWMath;

namespace WWIIRFilterDesign {
    /// <summary>
    /// Building block of IIR filter. used by IIRFilter class
    /// </summary>
    class IIRFilterBlockReal {
        private DelayReal mDelayX;
        private DelayReal mDelayY;
        private RationalPolynomial mH;
        private double[] mA;
        private double[] mB;

        public override string ToString() {
            return mH.ToString();
        }

        public IIRFilterBlockReal(RationalPolynomial p) {
            mH = p;
            mDelayX = new DelayReal(p.NumerOrder() + 1);
            mDelayY = new DelayReal(p.DenomOrder() + 1);

            mB = new double[p.NumerOrder() + 1];
            for (int i = 0; i < mB.Length; ++i) {
                mB[i] = p.N(i).real;
            }

            mA = new double[p.DenomOrder() + 1];
            mA[0] = 1;
            for (int i = 1; i < mA.Length; ++i) {
                mA[i] = -p.D(i).real;
            }
        }

        public double Filter(double x) {
            mDelayX.Filter(x);

            double y = 0;

            // Direct form 1 structure
            // Discrete-time signal processing 3rd edition pp.417 figure 6.14

            // まずフィードフォワードの計算。
            // ディレイから取り出した値に係数bを掛ける。
            for (int i = 0; i <= mH.NumerOrder(); ++i) {
                y += mB[i] * mDelayX.GetNth(i);
            }

            // フィードバックの計算。
            for (int i = 1; i <= mH.DenomOrder(); ++i) {
                y += mA[i] * mDelayY.GetNth(i - 1);
            }

            mDelayY.Filter(y);

            return y;
        }
    }
}

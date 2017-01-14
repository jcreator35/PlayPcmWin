using WWMath;

namespace WWIIRFilterDesign {
    /// <summary>
    /// Building block of IIR filter. used by IIRFilter class
    /// </summary>
    class IIRFilterBlock {
        private DelayC mDelayX;
        private DelayC mDelayY;
        private RationalPolynomial mH;
        private WWComplex[] mA;
        private WWComplex[] mB;

        public IIRFilterBlock(RationalPolynomial p) {
            mH = p;
            mDelayX = new DelayC(p.NumerOrder()+1);
            mDelayY = new DelayC(p.DenomOrder()+1);

            mB = new WWComplex[p.NumerOrder() + 1];
            for (int i = 0; i < mB.Length; ++i) {
                mB[i] = p.N(i);
            }

            mA = new WWComplex[p.DenomOrder() + 1];
            mA[0] = WWComplex.Unity();
            for (int i = 1; i < mA.Length; ++i) {
                mA[i] = WWComplex.Minus(p.D(i));
            }
        }

        public WWComplex Filter(WWComplex x) {
            mDelayX.Filter(x);

            WWComplex y = WWComplex.Zero();

            // まずフィードフォワードの計算。
            // ディレイから取り出した値に係数bを掛ける。
            for (int i = 0; i <= mH.NumerOrder(); ++i) {
                y = WWComplex.Add(y, WWComplex.Mul(mB[i], mDelayX.GetNth(i)));
            }

            // フィードバックの計算。
            for (int i = 1; i <= mH.DenomOrder(); ++i) {
                y = WWComplex.Add(y, WWComplex.Mul(mA[i], mDelayY.GetNth(i-1)));
            }

            mDelayY.Filter(y);

            return y;
        }
    }
}

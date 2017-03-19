using WWMath;

namespace WWIIRFilterDesign {
    /// <summary>
    /// Building block of IIR filter. used by IIRFilter class
    /// </summary>
    class IIRFilterBlockComplex {
        private DelayComplex mDelayX;
        private DelayComplex mDelayY;
        private ComplexRationalPolynomial mH;
        private WWComplex[] mA;
        private WWComplex[] mB;

        public override string ToString() {
            return mH.ToString();
        }
        
        public IIRFilterBlockComplex(ComplexRationalPolynomial p) {
            mH = p;
            mDelayX = new DelayComplex(p.NumerDegree()+1);
            mDelayY = new DelayComplex(p.DenomDegree()+1);

            mB = new WWComplex[p.NumerDegree() + 1];
            for (int i = 0; i < mB.Length; ++i) {
                mB[i] = p.N(i);
            }

            mA = new WWComplex[p.DenomDegree() + 1];
            mA[0] = WWComplex.Unity();
            for (int i = 1; i < mA.Length; ++i) {
                mA[i] = WWComplex.Minus(p.D(i));
            }
        }

        public WWComplex Filter(WWComplex x) {
            mDelayX.Filter(x);

            WWComplex y = WWComplex.Zero();
            
            // Direct form 1 structure
            // Discrete-time signal processing 3rd edition pp.417 figure 6.14

            // まずフィードフォワードの計算。
            // ディレイから取り出した値に係数bを掛ける。
            for (int i = 0; i <= mH.NumerDegree(); ++i) {
                y = WWComplex.Add(y, WWComplex.Mul(mB[i], mDelayX.GetNth(i)));
            }

            // フィードバックの計算。
            for (int i = 1; i <= mH.DenomDegree(); ++i) {
                y = WWComplex.Add(y, WWComplex.Mul(mA[i], mDelayY.GetNth(i-1)));
            }

            mDelayY.Filter(y);

            return y;
        }
    }
}

using System;

namespace WWMath {
    public class FirstOrderRationalPolynomial : RationalPolynomial {
        private WWComplex[] numer = new WWComplex[2];
        private WWComplex[] denom = new WWComplex[2];

        /// <summary>
        /// rational polynomial
        ///   n1x + n0
        ///  ----------
        ///   d1x + d0
        /// </summary>
        /// <param name="n1">numerator first order coefficient</param>
        /// <param name="n0">numerator zero order coefficient</param>
        /// <param name="d1">denominator first order coefficient</param>
        /// <param name="d0">denominator zero order coefficient</param>
        public FirstOrderRationalPolynomial(WWComplex n1, WWComplex n0, WWComplex d1, WWComplex d0) {
                if (d1.Magnitude() == 0 && d0.Magnitude() == 0) {
                    throw new DivideByZeroException();
                }

                numer[1] = n1;
                numer[0] = n0;
                denom[1] = d1;
                denom[0] = d0;
        }

        public override int Order() { return 1; }

        public override WWComplex[] NumeratorCoeffs() {
            return numer;
        }

        public override WWComplex[] DenominatorCoeffs() {
            return denom;
        }

        public override WWComplex N(int nth) {
            return numer[nth];
        }

        public override WWComplex D(int nth) {
            return denom[nth];
        }

        public override string ToString() {
            return ToString("x");
        }

        public override string ToString(string variableSymbol) {
            string n = WWUtil.PolynomialToString(numer[1], numer[0], variableSymbol);
            string d = WWUtil.PolynomialToString(denom[1], denom[0], variableSymbol);
            return string.Format("{{ {0} }} / {{ {1} }}", n, d);
        }

        /// <summary>
        /// 伝達関数の多項式を周波数スケーリングする。
        /// </summary>
        /// <param name="ωc">周波数 (rad/s)</param>
        public override void FrequencyScaling(double ωc) {
            /* 教科書には1次の項をωcで割ると書いてある。
             * しかし、分母のsの1乗の項を1にしたいので、分子と分母をωc倍する。
             * ということは結局、定数項をωc倍する。
             */
            numer[0].Mul(ωc);
            denom[0].Mul(ωc);
        }

        public FirstOrderRationalPolynomial CreateCopy() {
            return new FirstOrderRationalPolynomial(
                numer[1].CreateCopy(), numer[0].CreateCopy(),
                denom[1].CreateCopy(), denom[0].CreateCopy());
        }
    }
}

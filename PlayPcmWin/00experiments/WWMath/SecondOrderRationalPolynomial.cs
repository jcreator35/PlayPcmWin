using System;

namespace WWMath {
    public class SecondOrderRationalPolynomial : RationalPolynomial {
        private WWComplex[] numer = new WWComplex[3];
        private WWComplex[] denom = new WWComplex[3];

        /// <summary>
        /// rational polynomial
        ///   n2x^2 + n1x + n0
        ///  ------------------
        ///   d2x^2 + d1x + d0
        /// </summary>
        /// <param name="n2">numerator second order coefficient</param>
        /// <param name="n1">numerator first order coefficient</param>
        /// <param name="n0">numerator zero order coefficient</param>
        /// <param name="d2">denominator second order coefficient</param>
        /// <param name="d1">denominator first order coefficient</param>
        /// <param name="d0">denominator zero order coefficient</param>
        public SecondOrderRationalPolynomial(WWComplex n2, WWComplex n1, WWComplex n0,
                WWComplex d2, WWComplex d1, WWComplex d0) {
            if (d2.Magnitude() == 0 && d1.Magnitude() == 0 && d0.Magnitude() == 0) {
                throw new DivideByZeroException();
            }

            numer[2] = n2;
            numer[1] = n1;
            numer[0] = n0;

            denom[2] = d2;
            denom[1] = d1;
            denom[0] = d0;
        }

        public override int Order() { return 2; }

        public override WWComplex[] NumeratorCoeffs() {
            return numer;
        }

        public override WWComplex[] DenominatorCoeffs() {
            return denom;
        }

        public override WWComplex NumeratorCoeff(int nth) {
            return numer[nth];
        }

        public override WWComplex DenominatorCoeff(int nth) {
            return denom[nth];
        }

        public override string ToString() {
            return ToString("x");
        }

        public override string ToString(string variableSymbol) {
            string n = WWUtil.PolynomialToString(numer[2], numer[1], numer[0], variableSymbol);
            string d = WWUtil.PolynomialToString(denom[2], denom[1], denom[0], variableSymbol);
            return string.Format("{{ {0} }} / {{ {1} }}", n, d);
        }

        /// <summary>
        /// 伝達関数の多項式を周波数スケーリングする。
        /// </summary>
        /// <param name="ωc">周波数 (rad/s)</param>
        public override void FrequencyScaling(double ωc) {
            /* 教科書には2次の項をωc^2で割り、1次の項をωcで割ると書いてある。
             * しかし、分母のsの2乗の項を1にしたいので、分子と分母をωc^2倍する。
             * ということは結局、1次の項をωc倍、定数項をωc^2倍する。
             */
            numer[1].Mul(ωc);
            denom[1].Mul(ωc);
            numer[0].Mul(ωc * ωc);
            denom[0].Mul(ωc * ωc);
        }
    }
}

using System;

namespace WWMath {
    public class SecondOrderComplexRationalPolynomial : ComplexRationalPolynomial {
        private readonly WWComplex[] numer = new WWComplex[3];
        private readonly WWComplex[] denom = new WWComplex[3];

        /// <summary>
        /// rational poly
        ///   n2x^2 + n1x + n0
        ///  ------------------
        ///   d2x^2 + d1x + d0
        /// </summary>
        /// <param name="n2">numerator second orderPlus1 coefficient</param>
        /// <param name="n1">numerator first orderPlus1 coefficient</param>
        /// <param name="n0">numerator zero orderPlus1 coefficient</param>
        /// <param name="d2">denominator second orderPlus1 coefficient</param>
        /// <param name="d1">denominator first orderPlus1 coefficient</param>
        /// <param name="d0">denominator zero orderPlus1 coefficient</param>
        public SecondOrderComplexRationalPolynomial(WWComplex n2, WWComplex n1, WWComplex n0,
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
        
        private static bool AlmostZero(double v) {
            return Math.Abs(v) < 1e-8;
        }

        public RealRationalPolynomial ToRealPolynomial() {
            var n = new double[NumerDegree() + 1];
            for (int i = 0; i < n.Length; ++i) {
                n[i] = numer[i].real;
                System.Diagnostics.Debug.Assert(AlmostZero(numer[i].imaginary));
            }

            var d = new double[DenomDegree() + 1];
            for (int i = 0; i < d.Length; ++i) {
                d[i] = denom[i].real;
                System.Diagnostics.Debug.Assert(AlmostZero(denom[i].imaginary));
            }
            return new RealRationalPolynomial(n, d);
        }

        public override int Degree() { return 2; }
        
        public override int NumerDegree() {
            if (numer[2].Magnitude() != 0) {
                return 2;
            }
            if (numer[1].Magnitude() != 0) {
                return 1;
            }
            return 0;
        }

        public override int DenomDegree() {
            if (denom[2].Magnitude() != 0) {
                return 2;
            }
            if (denom[1].Magnitude() != 0) {
                return 1;
            }
            return 0;
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
            string n = WWUtil.PolynomialToString(numer[2], numer[1], numer[0], variableSymbol);
            string d = WWUtil.PolynomialToString(denom[2], denom[1], denom[0], variableSymbol);
            return string.Format("{{ {0} }} / {{ {1} }}", n, d);
        }
    }
}

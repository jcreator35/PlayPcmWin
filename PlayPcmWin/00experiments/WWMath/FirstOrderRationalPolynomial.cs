using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWMath {
    public class FirstOrderRationalPolynomial {
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

        public WWComplex[] NumeratorCoeffs() {
            return numer;
        }

        public WWComplex[] DenominatorCoeffs() {
            return denom;
        }

        public WWComplex NumeratorCoeff(int nth) {
            return numer[nth];
        }

        public WWComplex DenominatorCoeff(int nth) {
            return denom[nth];
        }

        /// <summary>
        /// output string represents "c1x + c0"
        /// </summary>
        private string PolynomialToString(WWComplex c1, WWComplex c0, string variableSymbol) {
            if (c1.Magnitude() == 0) {
                return string.Format("{0}", c0);
            }

            if (c0.Magnitude() == 0) {
                return string.Format("({0}){1}", c1, variableSymbol);
            }

            return string.Format("({0}){1} + ({2})", c1, variableSymbol, c0);
        }

        public override string ToString() {
            return ToString("x");
        }

        public string ToString(string variableSymbol) {
            string n = PolynomialToString(numer[1], numer[0], variableSymbol);
            string d = PolynomialToString(denom[1], denom[0], variableSymbol);
            return string.Format("｛{0}｝/｛{1}｝", n, d);
        }
    }
}

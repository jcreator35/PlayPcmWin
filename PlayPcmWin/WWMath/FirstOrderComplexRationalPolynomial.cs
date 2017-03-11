using System;
using System.Collections.Generic;

namespace WWMath {
    public class FirstOrderComplexRationalPolynomial : ComplexRationalPolynomial {
        private readonly WWComplex[] numer = new WWComplex[2];
        private readonly WWComplex[] denom = new WWComplex[2];

        /// <summary>
        /// rational polynomial
        ///   n1x + n0
        ///  ----------
        ///   d1x + d0
        /// </summary>
        /// <param name="n1">numerator first orderPlus1 coefficient</param>
        /// <param name="n0">numerator zero orderPlus1 coefficient</param>
        /// <param name="d1">denominator first orderPlus1 coefficient</param>
        /// <param name="d0">denominator zero orderPlus1 coefficient</param>
        public FirstOrderComplexRationalPolynomial(WWComplex n1, WWComplex n0, WWComplex d1, WWComplex d0) {
                if (d1.Magnitude() == 0 && d0.Magnitude() == 0) {
                    throw new DivideByZeroException();
                }

                numer[1] = n1;
                numer[0] = n0;
                denom[1] = d1;
                denom[0] = d0;
        }

        public override int Order() { return 1; }

        public override int NumerOrder() {
            if (numer[1].Magnitude() == 0) {
                return 0;
            }
            return 1;
        }

        public override int DenomOrder() {
            if (denom[1].Magnitude() == 0) {
                return 0;
            }
            return 1;
        }

        public override WWComplex N(int nth) {
            return numer[nth];
        }

        public override WWComplex D(int nth) {
            return denom[nth];
        }

        public override string ToString() {
            return ToString("x", "i");
        }

        public override string ToString(string variableSymbol, string imaginaryUnit) {
            string n = WWUtil.PolynomialToString(numer[1], numer[0], variableSymbol, imaginaryUnit);
            string d = WWUtil.PolynomialToString(denom[1], denom[0], variableSymbol, imaginaryUnit);
            return string.Format("{{ {0} }} / {{ {1} }}", n, d);
        }

        /// <summary>
        /// 1次多項式を作る。
        /// mCoeffs[0] + x * mCoeffs[1]
        /// </summary>
        public static List<FirstOrderComplexRationalPolynomial> CreateFromCoeffList(WWComplex [] coeffList) {
            var p = new List<FirstOrderComplexRationalPolynomial>();
            if (coeffList.Length == 0) {
                return p;
            }
            if (coeffList.Length == 1) {
                // 定数を追加。
                p.Add(new FirstOrderComplexRationalPolynomial(WWComplex.Zero(), coeffList[0], WWComplex.Zero(), WWComplex.Unity()));
            }
            if (coeffList.Length == 2) {
                // 1次式を追加。
                p.Add(new FirstOrderComplexRationalPolynomial(coeffList[1], coeffList[0], WWComplex.Zero(), WWComplex.Unity()));
            }

            return p;
        }

        /// <summary>
        /// 2つの1次多項式のリストを足したリストを戻す。
        /// </summary>
        public static List<FirstOrderComplexRationalPolynomial> Add(List<FirstOrderComplexRationalPolynomial> lhs, List<FirstOrderComplexRationalPolynomial> rhs) {
            var rv = new List<FirstOrderComplexRationalPolynomial>();

            foreach (var i in lhs) {
                rv.Add(i);
            }
            foreach (var i in rhs) {
                rv.Add(i);
            }

            return rv;
        }
    }
}

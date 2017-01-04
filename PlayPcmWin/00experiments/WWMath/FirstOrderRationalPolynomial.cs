using System;
using System.Collections.Generic;

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
        /// <param name="n1">numerator first orderPlus1 coefficient</param>
        /// <param name="n0">numerator zero orderPlus1 coefficient</param>
        /// <param name="d1">denominator first orderPlus1 coefficient</param>
        /// <param name="d0">denominator zero orderPlus1 coefficient</param>
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

        public FirstOrderRationalPolynomial CreateCopy() {
            return new FirstOrderRationalPolynomial(
                numer[1], numer[0],
                denom[1], denom[0]);
        }

        /// <summary>
        /// 1次多項式を作る。
        /// coeffList[0] + x * coeffList[1]
        /// </summary>
        public static List<FirstOrderRationalPolynomial> CreateFromCoeffList(List<WWComplex> coeffList) {
            var p = new List<FirstOrderRationalPolynomial>();
            if (coeffList.Count == 0) {
                return p;
            }
            if (coeffList.Count == 1) {
                // 定数を追加。
                p.Add(new FirstOrderRationalPolynomial(WWComplex.Zero(), coeffList[0], WWComplex.Zero(), WWComplex.Unity()));
            }
            if (coeffList.Count == 2) {
                // 1次式を追加。
                p.Add(new FirstOrderRationalPolynomial(coeffList[1], coeffList[0], WWComplex.Zero(), WWComplex.Unity()));
            }

            return p;
        }

        /// <summary>
        /// 2つの1次多項式のリストを足したコピーを作成する。引数のリストの内容は変更しない。
        /// </summary>
        public static List<FirstOrderRationalPolynomial> Add(List<FirstOrderRationalPolynomial> lhs, List<FirstOrderRationalPolynomial> rhs) {
            var rv = new List<FirstOrderRationalPolynomial>();

            foreach (var i in lhs) {
                rv.Add(i.CreateCopy());
            }
            foreach (var i in rhs) {
                rv.Add(i.CreateCopy());
            }

            return rv;
        }
    }
}

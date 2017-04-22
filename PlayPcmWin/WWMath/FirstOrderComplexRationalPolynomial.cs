using System;
using System.Collections.Generic;

namespace WWMath {
    public class FirstOrderComplexRationalPolynomial : ComplexRationalPolynomial {
        private readonly WWComplex[] numer = new WWComplex[2];
        private readonly WWComplex[] denom = new WWComplex[2];

        /// <summary>
        /// rational poly
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

        /// <summary>
        /// 分子と分母を定数倍したrational functionを戻す。
        /// 式の内容は変わらない。
        /// </summary>
        public FirstOrderComplexRationalPolynomial ScaleAllCoeffs(double c) {
            return new FirstOrderComplexRationalPolynomial(
                numer[1].Scale(c), numer[0].Scale(c),
                denom[1].Scale(c), denom[0].Scale(c));
        }

        /// <summary>
        /// 分子と分母を定数倍したrational functionを戻す。
        /// 式の内容は変わらない。
        /// </summary>
        public FirstOrderComplexRationalPolynomial ScaleAllCoeffs(WWComplex c) {
            return new FirstOrderComplexRationalPolynomial(
                WWComplex.Mul(numer[1], c), WWComplex.Mul(numer[0], c),
                WWComplex.Mul(denom[1], c), WWComplex.Mul(denom[0], c));
        }

        /// <summary>
        /// 分子だけを定数倍したrational functionを戻す。
        /// </summary>
        public FirstOrderComplexRationalPolynomial ScaleNumeratorCoeffs(double s) {
            var n = new WWComplex[numer.Length];
            for (int i=0; i < n.Length; ++i) {
                n[i] = WWComplex.Mul(numer[i], s);
            }
            return new FirstOrderComplexRationalPolynomial(n[1], n[0], denom[1], denom[0]);
        }

        public override int Degree() { return 1; }

        public override int NumerDegree() {
            if (numer[1].Magnitude() == 0) {
                return 0;
            }
            return 1;
        }

        public override int DenomDegree() {
            if (denom[1].Magnitude() == 0) {
                return 0;
            }
            return 1;
        }

        public WWComplex[] NumerCoeffs() {
            WWComplex[] n = new WWComplex[numer.Length];
            Array.Copy(numer, n, n.Length);
            return n;
        }

        public WWComplex[] DenomCoeffs() {
            WWComplex[] d = new WWComplex[denom.Length];
            Array.Copy(denom, d, d.Length);
            return d;
        }

        public override WWComplex N(int nth) {
            return numer[nth];
        }

        public override WWComplex D(int nth) {
            return denom[nth];
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

        public WWComplex Evaluate(WWComplex x) {
            WWComplex yN = WWComplex.Add(WWComplex.Mul(x, numer[1]), numer[0]);
            WWComplex yD = WWComplex.Add(WWComplex.Mul(x, denom[1]), denom[0]);
            return WWComplex.Div(yN, yD);
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
        /// 1次多項式を作る。
        /// mCoeffs[0] + p * mCoeffs[1]
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

using System;
using System.Collections.Generic;
using System.Text;

namespace WWMath {
    public class WWPolynomial {
        /// <summary>
        /// 1次有理多項式 x 1次有理多項式
        /// </summary>
        public static SecondOrderRationalPolynomial
        Mul(FirstOrderComplexRationalPolynomial lhs, FirstOrderComplexRationalPolynomial rhs) {
            // 分子の項 x 分子の項
            var n2 = WWComplex.Mul(lhs.N(1), rhs.N(1));
            var n1 = WWComplex.Add(WWComplex.Mul(lhs.N(1), rhs.N(0)),
                                   WWComplex.Mul(lhs.N(0), rhs.N(1)));
            var n0 = WWComplex.Mul(lhs.N(0), rhs.N(0));

            // 分母の項 x 分母の項
            var d2 = WWComplex.Mul(lhs.D(1), rhs.D(1));
            var d1 = WWComplex.Add(WWComplex.Mul(lhs.D(1), rhs.D(0)),
                                   WWComplex.Mul(lhs.D(0), rhs.D(1)));
            var d0 = WWComplex.Mul(lhs.D(0), rhs.D(0));

            return new SecondOrderRationalPolynomial(n2, n1, n0, d2, d1, d0);
        }
        
        /// <summary>
        /// 1次有理多項式 + 1次有理多項式
        /// </summary>
        public static SecondOrderRationalPolynomial
        Add(FirstOrderComplexRationalPolynomial L, FirstOrderComplexRationalPolynomial R) {
            /*  nL     nR
             * x+2    x+4
             * ─── + ────
             * x+1    x+3
             *  dL     dR
             *  
             *         nL   dR      nR   dL
             * 分子 = (x+2)(x+3) + (x+4)(x+1) = (x^2+5x+6) + (x^2+5x+4) = 2x^2+10x+10
             * 分子の次数 = 2
             * 
             *         dL   dR
             * 分母 = (x+1)(x+3) = x^2 + (1+3)x + 1*3
             * 分母の次数 = 2
             */

            // 分子の項
            var n2 = WWComplex.Add(
                WWComplex.Mul(L.N(1), R.D(1)),
                WWComplex.Mul(R.N(1), L.D(1)));

            var n1 = WWComplex.Add(
                WWComplex.Add(WWComplex.Mul(L.N(0), R.D(1)),
                              WWComplex.Mul(L.N(1), R.D(0))),
                WWComplex.Add(WWComplex.Mul(R.N(0), L.D(1)),
                              WWComplex.Mul(R.N(1), L.D(0))));

            var n0 = WWComplex.Add(
                WWComplex.Mul(L.N(0), R.D(0)),
                WWComplex.Mul(R.N(0), L.D(0)));

            // 分母の項
            var d2 = WWComplex.Mul(L.D(1), R.D(1));

            var d1 = WWComplex.Add(WWComplex.Mul(L.D(0), R.D(1)),
                                   WWComplex.Mul(L.D(1), R.D(0)));

            var d0 = WWComplex.Mul(L.D(0), R.D(0));

            return new SecondOrderRationalPolynomial(n2, n1, n0, d2, d1, d0);
        }

        public static HighOrderComplexRationalPolynomial
        Add(HighOrderComplexRationalPolynomial lhs, FirstOrderComplexRationalPolynomial rhs) {
            /*  nL          nR
             * x^2+2x+2    x+4
             * ───────── + ────
             * x^2+ x+1    x+3
             *  dL          dR
             *  
             *          dL       dR
             * 分母 = (x^2+x+1) (x+3)
             * 分母の次数 = order(dL) + order(dR) + 1
             * 
             *          nL       dR      nR    dL
             * 分子 = (x^2+2x+2)(x+3) + (x+4)(x^2+x+1)
             * 分子の次数 = order(nL) + order(dR) + 1 か order(nR) * order(dL) + 1の大きいほう
             */

            ComplexPolynomial denomL;
            {
                var d = new WWComplex[lhs.DenomOrder() + 1];
                for (int i = 0; i <= lhs.DenomOrder(); ++i) {
                    d[i] = lhs.D(i);
                }
                denomL = new ComplexPolynomial(d);
            }
            ComplexPolynomial numerL;
            {
                var n = new WWComplex[lhs.NumerOrder() + 1];
                for (int i = 0; i <= lhs.NumerOrder(); ++i) {
                    n[i] = lhs.N(i);
                }
                numerL = new ComplexPolynomial(n);
            }

            // 分母の項
            ComplexPolynomial denomResult;
            {
                var denomX = new ComplexPolynomial(new WWComplex[0]);
                if (1 == rhs.DenomOrder()) {
                    denomX = ComplexPolynomial.MultiplyC(ComplexPolynomial.MultiplyX(denomL), rhs.D(1));
                }
                var denomC = ComplexPolynomial.MultiplyC(denomL, rhs.D(0));
                denomResult = ComplexPolynomial.Add(denomX, denomC);
            }

            // 分子の項
            ComplexPolynomial numerResult;
            {
                ComplexPolynomial numer0;
                {
                    var numerX0 = new ComplexPolynomial(new WWComplex[0]);
                    if (1 == rhs.DenomOrder()) {
                        numerX0 = ComplexPolynomial.MultiplyC(ComplexPolynomial.MultiplyX(numerL), rhs.D(1));
                    }
                    var numerC0 = ComplexPolynomial.MultiplyC(numerL, rhs.D(0));
                    numer0 = ComplexPolynomial.Add(numerX0, numerC0);
                }

                ComplexPolynomial numer1;
                {
                    var numerX1 = new ComplexPolynomial(new WWComplex[0]);
                    if (1 == rhs.NumerOrder()) {
                        numerX1 = ComplexPolynomial.MultiplyC(ComplexPolynomial.MultiplyX(denomL), rhs.N(1));
                    }
                    var numerC1 = ComplexPolynomial.MultiplyC(denomL, rhs.N(0));
                    numer1 = ComplexPolynomial.Add(numerX1, numerC1);
                }

                numerResult = ComplexPolynomial.Add(numer0, numer1);
            }

            return new HighOrderComplexRationalPolynomial(numerResult.ToArray(), denomResult.ToArray());
        }

        /// <summary>
        /// 多項式の根のリストと定数倍のパラメーターを受け取って多項式のn乗の項のリストを戻す。
        /// c(x-b[0])(x-b[1]) → c(x^2-(b[0]+b[1])x+b[0]b[1])
        /// rv[2] := c
        /// rv[1] := -c*(b[0]+b[1])
        /// rv[0] := c*(b[0]b[1])
        /// </summary>
        /// <param name="rootList">多項式の根のリスト</param>
        /// <param name="constantMultiplier">定数倍のパラメーター()例参照</param>
        /// <returns></returns>
        public static WWComplex []
        RootListToCoeffList(WWComplex [] b, WWComplex c) {
            var coeff = new List<WWComplex>();
            if (b.Length == 0) {
                // 定数項のみ。(?)
                coeff.Add(c);
                return coeff.ToArray();
            }

            var b2 = new List<WWComplex>();
            foreach (var i in b) {
                b2.Add(i);
            }

            // (x-b[0])
            coeff.Add(WWComplex.Mul(b2[0],-1));
            coeff.Add(WWComplex.Unity());

            b2.RemoveAt(0);

            WWComplex[] coeffArray = coeff.ToArray();
            while (0 < b2.Count) {
                // 多項式に(x-b[k])を掛ける。
                var s1 = ComplexPolynomial.MultiplyX(new ComplexPolynomial(coeffArray));
                var s0 = ComplexPolynomial.MultiplyC(new ComplexPolynomial(coeffArray),
                    WWComplex.Mul(b2[0],-1));
                coeffArray = ComplexPolynomial.Add(s1, s0).ToArray();
                b2.RemoveAt(0);
            }

            return ComplexPolynomial.MultiplyC(new ComplexPolynomial(coeffArray), c).ToArray();
        }

        public class PolynomialAndRationalPolynomial {
            public WWComplex [] coeffList;
            public WWComplex [] numerCoeffList;
            public WWComplex [] denomRootList;

            public void Print(string x) {
                if (0 < coeffList.Length) {
                    bool bFirst = true;
                    for (int i=coeffList.Length-1; 0<=i;--i) {
                        if (coeffList[i].AlmostZero()) {
                            continue;
                        }

                        if (bFirst) {
                            bFirst = false;
                        } else {
                            Console.Write(" + ");
                        }

                        if (i == 0) {
                            Console.Write("{0}", coeffList[i]);
                        } else if (i == 1) {
                            Console.Write("{0}*{1}", coeffList[i], x);
                        } else {
                            Console.Write("{0}*({1}^{2})", coeffList[i], x, i);
                        }
                    }
                    if (!bFirst) {
                        Console.Write(" + ");
                    }
                }

                Console.Write(" { ");
                {
                    bool bFirst = true;
                    for (int i = numerCoeffList.Length-1; 0<=i;--i) {
                        if (numerCoeffList[i].AlmostZero()) {
                            continue;
                        }

                        if (bFirst) {
                            bFirst = false;
                        } else {
                            Console.Write(" + ");
                        }
                        if (i == 0) {
                            Console.Write("{0}", numerCoeffList[i]);
                        } else if (i == 1) {
                            Console.Write("{0}*{1}", numerCoeffList[i], x);
                        } else {
                            Console.Write("{0}*({1}^{2})", numerCoeffList[i], x, i);
                        }
                    }
                }

                Console.Write(" } / { ");

                {
                    for (int i = 0; i < denomRootList.Length; ++i) {
                        if (denomRootList[i].AlmostZero()) {
                            Console.WriteLine(" {0} ", x);
                            continue;
                        }

                        Console.Write("({0}+({1}))", x, WWComplex.Minus(denomRootList[i]));
                    }
                }
                Console.WriteLine(" }");
            }
        };

        /// <summary>
        /// 有理多項式の約分をして定数項以上の項を分離、有理多項式の分子の次数を分母の次数未満に減らす。
        ///  x-1        (x+1) -(x+1) + (x-1)            -2
        /// ─────  ⇒  ──────────────────────  ⇒  1 + ─────
        ///  x+1                  x+1                   x+1
        /// 
        /// </summary>
        public static PolynomialAndRationalPolynomial
        Reduction(WWComplex [] aNumerCoeffList, WWComplex [] aDenomRootList) {
            var rv = new PolynomialAndRationalPolynomial();
            rv.coeffList = new WWComplex[0];
            rv.numerCoeffList = new WWComplex[aNumerCoeffList.Length];
            Array.Copy(aNumerCoeffList, rv.numerCoeffList, aNumerCoeffList.Length);
            rv.denomRootList = new WWComplex[aDenomRootList.Length];
            Array.Copy(aDenomRootList, rv.denomRootList, aDenomRootList.Length);

            if (rv.numerCoeffList.Length - 1 < rv.denomRootList.Length) {
                // 既に分子の多項式の次数が分母の多項式の次数よりも低い。
                return rv;
            }

            // 分母の根のリスト⇒分母の多項式の係数のリストに変換。
            var denomCoeffList = RootListToCoeffList(aDenomRootList, new WWComplex(1, 0));

            var quotient = new List<WWComplex>();

            while (denomCoeffList.Length <= rv.numerCoeffList.Length) {
                // denomの最も次数が高い項の係数がcdn、numerの最も次数が高い項の係数がcnnとすると
                // denomiCoeffListを-cnn/cdn * s^(numerの次数-denomの次数)倍してnumerCoeffListと足し合わせてnumerの次数を1下げる。
                // このときrv.coeffListにc == cnn/cdnを足す。
                var c = WWComplex.Div(rv.numerCoeffList[rv.numerCoeffList.Length - 1],
                        denomCoeffList[denomCoeffList.Length - 1]);
                quotient.Insert(0, c);
                var denomMulX = new ComplexPolynomial(denomCoeffList);
                while (denomMulX.Order()+1 < rv.numerCoeffList.Length) {
                    denomMulX = ComplexPolynomial.MultiplyX(denomMulX);
                }
                denomMulX = ComplexPolynomial.MultiplyC(denomMulX, WWComplex.Mul(c,-1));

                // ここで引き算することで分子の多項式の次数が1減る。
                int expectedOrder = rv.numerCoeffList.Length - 2;
                rv.numerCoeffList = ComplexPolynomial.TrimPolynomialOrder(
                    ComplexPolynomial.Add(denomMulX, new ComplexPolynomial(rv.numerCoeffList)),
                    expectedOrder).ToArray();

                // 引き算によって一挙に2以上次数が減った場合coeffListに0を足す。
                int actualOrder = rv.numerCoeffList.Length - 1;
                for (int i=0; i < expectedOrder - actualOrder; ++i) {
                    quotient.Insert(0, new WWComplex(0, 0));
                }
            }

            rv.coeffList = quotient.ToArray();
            return rv;
        }

        /// <summary>
        /// p次オールポールの多項式(分子は多項式係数のリストで分母は根のリスト)を部分分数展開する。分子の多項式の次数はp次未満。
        ///
        ///             ... + nCoeffs[2]s^2 + nCoeffs[1]s + nCoeffs[0]
        /// F(s) = ----------------------------------------------------------
        ///          (s-denomR[0])(s-denomR[1])(s-denomR[3])…(s-denomR[p-1])
        ///          
        /// 戻り値は、多項式を部分分数分解した結果の、1次有理多項式のp個のリスト。
        /// 
        ///             c[0]            c[1]            c[2]             c[p-1]
        /// F(s) = ------------- + ------------- + ------------- + … + ---------------------
        ///         s-denomR[0]     s-denomR[1]     s-denomR[2]         s-denomR[p-1]
        /// </summary>
        public static List<FirstOrderComplexRationalPolynomial>
        PartialFractionDecomposition(
                WWComplex [] nCoeffs, WWComplex [] dRoots) {
            var result = new List<FirstOrderComplexRationalPolynomial>();

            if (dRoots.Length == 1 && nCoeffs.Length == 1) {
                result.Add(new FirstOrderComplexRationalPolynomial(WWComplex.Zero(),
                    nCoeffs[0], WWComplex.Unity(), WWComplex.Minus(dRoots[0])));
                return result;
            }

            if (dRoots.Length < 2) {
                throw new ArgumentException("denomR");
            }
            if (dRoots.Length < nCoeffs.Length) {
                throw new ArgumentException("nCoeffs");
            }

            for (int k = 0; k < dRoots.Length; ++k) {
                // cn = ... + nCoeffs[2]s^2 + nCoeffs[1]s + nCoeffs[0]
                // 係数c[0]は、s==denomR[0]としたときの、cn/(s-denomR[1])(s-denomR[2])(s-denomR[3])…(s-denomR[p-1])
                // 係数c[1]は、s==denomR[1]としたときの、cn/(s-denomR[0])(s-denomR[2])(s-denomR[3])…(s-denomR[p-1])
                // 係数c[2]は、s==denomR[2]としたときの、cn/(s-denomR[0])(s-denomR[1])(s-denomR[3])…(s-denomR[p-1])

                // 分子の値c。
                var c = WWComplex.Zero();
                var s = WWComplex.Unity();
                for (int j = 0; j < nCoeffs.Length; ++j) {
                    c = WWComplex.Add(c, WWComplex.Mul(nCoeffs[j], s));
                    s = WWComplex.Mul(s, dRoots[k]);
                }

                for (int i = 0; i < dRoots.Length; ++i) {
                    if (i == k) {
                        continue;
                    }

                    c = WWComplex.Div(c, WWComplex.Sub(dRoots[k], dRoots[i]));
                }

                result.Add(new FirstOrderComplexRationalPolynomial(
                    WWComplex.Zero(), c,
                    WWComplex.Unity(), WWComplex.Minus(dRoots[k])));
            }

            return result;
        }

        public class AllRealDivisionResult {
            public RealPolynomial quotient;
            public RealRationalPolynomial remainder;
        };

        /// <summary>
        /// Algebraic long division of all-real polynomial by all-real polynomial
        /// https://www.khanacademy.org/math/algebra-home/alg-polynomials/alg-long-division-of-polynomials/v/polynomial-division
        /// </summary>
        public static AllRealDivisionResult
        AlgebraicLongDivision(RealPolynomial dividend, RealPolynomial divisor) {
            if (dividend.Order() < divisor.Order()) {
                throw new ArgumentException("dividendCoef order is smaller than divisorCoef");
            }
            AllRealDivisionResult r = new AllRealDivisionResult();

            var quotient = new double [dividend.Order() - divisor.Order() + 1];
            var divRemain = new double[dividend.Order()+1];
            for (int i = 0; i < divRemain.Length; ++i) {
                divRemain[i] = dividend.C(i);
            }
            for (int i = 0; i<quotient.Length; ++i) {
                double q = divRemain[dividend.Order() - i] 
                    / divisor.C(divisor.Order() - 1);
                quotient[quotient.Length - 1 - i] = q;
                for (int j = 0; j < divisor.Order()+1; ++j) {
                    divRemain[dividend.Order() - i - j] -=
                        q * divisor.C(divisor.Order() - j);
                }
            }

            r.quotient = new RealPolynomial(quotient);
            r.remainder = new RealRationalPolynomial(
                new RealPolynomial(divRemain).ReduceOrder(),
                divisor.ReduceOrder());
            return r;
        }

        public static void Test() {
            {
                /*
                 * 部分分数分解のテスト。
                 *           s + 3
                 * X(s) = -------------
                 *         (s+1)s(s-2)
                 * 
                 * 部分分数分解すると
                 * 
                 *          2/3      -3/2      5/6
                 * X(s) = ------- + ------ + -------
                 *         s + 1       s      s - 2
                 */
                var numerCoeffs = new WWComplex [] {
                    new WWComplex(3, 0),
                    new WWComplex(1, 0)};

                var dRoots = new WWComplex [] {
                    new WWComplex(-1, 0),
                    new WWComplex(0, 0),
                    new WWComplex(2, 0)};

                var p = WWPolynomial.PartialFractionDecomposition(numerCoeffs, dRoots);

                for (int i = 0; i < p.Count; ++i) {
                    Console.WriteLine(p[i].ToString("s", "i"));
                    if (i != p.Count - 1) {
                        Console.WriteLine(" + ");
                    }
                }

                Console.WriteLine("");
            }

            {
                // 約分のテスト
                //  x-1        (x+1) -(x+1) + (x-1)            -2
                // ─────  ⇒  ──────────────────────  ⇒  1 + ─────
                //  x+1                  x+1                   x+1

                var numerC = new List<WWComplex>();
                numerC.Add(new WWComplex(-1, 0)); // 定数項。
                numerC.Add(WWComplex.Unity());         // 1乗の項。

                var denomR = new List<WWComplex>();
                denomR.Add(new WWComplex(-1, 0));

                var r = Reduction(numerC.ToArray(), denomR.ToArray());
                r.Print("x");
            }

            {
                // 約分のテスト
                //  x^2+3x+2            (x^2+3x+2) -(x^2+7x+12)            -4x-10
                // ────────────  ⇒ 1+ ─────────────────────────  ⇒  1 + ────────────
                //  (x+3)(x+4)           x^2+7x+12                         (x+3)(x+4)

                var numerC = new WWComplex [] {
                    new WWComplex(2, 0), // 定数項。
                    new WWComplex(3, 0), // 1乗の項。
                    WWComplex.Unity()};  // 2乗の項。

                var denomR = new WWComplex [] {
                    new WWComplex(-3, 0),
                    new WWComplex(-4, 0)};

                var r = Reduction(numerC, denomR);
                r.Print("x");
            }
            {
                // 部分分数分解。
                //  -4x-10             2      -6
                // ────────────  ⇒  ───── + ─────
                //  (x+3)(x+4)        x+3     x+4

                var numerC = new WWComplex [] {
                    new WWComplex(-10, 0),
                    new WWComplex(-4, 0)};

                var denomR = new WWComplex [] {
                    new WWComplex(-3, 0),
                    new WWComplex(-4, 0)};

                var p = WWPolynomial.PartialFractionDecomposition(numerC, denomR);

                for (int i = 0; i < p.Count; ++i) {
                    Console.WriteLine(p[i].ToString("s", "i"));
                    if (i != p.Count - 1) {
                        Console.WriteLine(" + ");
                    }
                }

                Console.WriteLine("");
            }

            {
                var deriv = new RealPolynomial(new double[] { 1, 1, 1, 1 }).Derivative();
                Console.WriteLine("derivative of x^3+x^2+x+1={0}",
                    deriv.ToString("x"));
            }

            {
                var r2 = AlgebraicLongDivision(new RealPolynomial(new double[] { 6, 3, 1 }),
                    new RealPolynomial(new double[] { 1, 1}));
                Console.WriteLine("(x^2+3x+6)/(x+1) = {0} r {1}",
                    r2.quotient.ToString(), r2.remainder.ToString());
            }
        }
    }
}

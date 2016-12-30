using System;
using System.Collections.Generic;

namespace WWMath {
    public class WWPolynomial {
        /// <summary>
        /// 1次有理多項式 x 1次有理多項式
        /// </summary>
        public static SecondOrderRationalPolynomial Mul(FirstOrderRationalPolynomial lhs, FirstOrderRationalPolynomial rhs) {
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
        /// 多項式の係数のリスト(変数x)を受け取ってx倍して戻す。引数のcoeffListの内容は変更しない。
        /// </summary>
        public static List<WWComplex> MultiplyX(List<WWComplex> coeffList) {
            var rv = new List<WWComplex>();
            rv.Add(new WWComplex(0,0));
            for (int i = 0; i < coeffList.Count; ++i) {
                rv.Add(new WWComplex(coeffList[i]));
            }
            return rv;
        }

        /// <summary>
        /// 多項式の係数のリストを受け取り、全体をc倍して戻す。引数のcoeffListの内容は変更しない。
        /// </summary>
        public static List<WWComplex> MultiplyC(List<WWComplex> coeffList, WWComplex c) {
            var rv = new List<WWComplex>();
            for (int i = 0; i < coeffList.Count;++i) {
                rv.Add(new WWComplex(coeffList[i]).Mul(c));
            }
            return rv;
        }

        /// <summary>
        /// 多項式の次数を強制的にn次にする。n+1次以上の項を消す
        /// </summary>
        /// <param name="coeffList">多項式の係数のリスト。coeffList[0]:定数項、coeffList[1]:1次の項。</param>
        /// <param name="n">次数。0=定数のみ。1==定数と1次の項。</param>
        private static List<WWComplex> TrimPolynomialOrder(List<WWComplex> coeffList, int n) {
            var rv = new List<WWComplex>();
            for (int i = 0; i <= n; ++i) {
                if (coeffList.Count <= i) {
                    break;
                }
                rv.Add(new WWComplex(coeffList[i]));
            }

            //強制的に次数を減らした結果最高位の係数が0になったとき次数を減らす処理。
            rv = ReduceOrder(rv);
            return rv;
        }

        /// <summary>
        /// 最高次の項が0のとき削除して次数を減らす。
        /// </summary>
        private static List<WWComplex> ReduceOrder(List<WWComplex> coeffList) {
            var rv = new List<WWComplex>();
            for (int i = 0; i < coeffList.Count; ++i) {
                rv.Add(new WWComplex(coeffList[i]));
            }

            for (int i=rv.Count - 1; 0 < i; --i) {
                if (rv[i].AlmostZero()) {
                    rv.RemoveAt(i);
                } else {
                    break;
                }
            }

            return rv;
        }

        /// <summary>
        /// 2つの多項式の和を戻す。
        /// </summary>
        /// <param name="lhs">多項式の係数のリストl</param>
        /// <param name="rhs">多項式の係数のリストr</param>
        /// <returns>l+r</returns>
        public static List<WWComplex> Add(List<WWComplex> lhs, List<WWComplex> rhs) {
            var rv = new List<WWComplex>();
            int orderPlus1 = lhs.Count;
            if (orderPlus1 < rhs.Count) {
                orderPlus1 = rhs.Count;
            }

            for (int i=0; i < orderPlus1; ++i) {
                WWComplex ck = new WWComplex(0, 0);
                if (i < lhs.Count) {
                    ck.Add(lhs[i]);
                }
                if (i < rhs.Count) {
                    ck.Add(rhs[i]);
                }
                rv.Add(ck);
            }

            // 最高次の項が0のとき削除して次数を減らす。
            rv = ReduceOrder(rv);

            return rv;
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
        public static List<WWComplex> RootListToCoeffList(List<WWComplex> b, WWComplex c) {
            var b2 = new List<WWComplex>();
            foreach (var i in b) {
                b2.Add(i);
            }

            // (x-b[0])
            var coeff = new List<WWComplex>();
            coeff.Add(new WWComplex(b2[0]).Mul(-1));
            coeff.Add(new WWComplex(1, 0));

            b2.RemoveAt(0);

            while (0 < b2.Count) {
                // 多項式に(x-b[k])を掛ける。
                var s1 = MultiplyX(coeff);
                var s0 = MultiplyC(coeff, new WWComplex(b2[0]).Mul(-1));
                coeff = Add(s1, s0);
                b2.RemoveAt(0);
            }

            return MultiplyC(coeff, c);
        }

        public class PolynomialAndRationalPolynomial {
            public List<WWComplex> coeffList;
            public List<WWComplex> numerCoeffList;
            public List<WWComplex> denomRootList;

            public void Print(string x) {
                if (0 < coeffList.Count) {
                    bool bFirst = true;
                    for (int i = 0; i < coeffList.Count; ++i) {
                        if (coeffList[i].AlmostZero()) {
                            continue;
                        }

                        if (bFirst) {
                            bFirst = false;
                        } else {
                            Console.Write(" + ");
                        }
                        Console.Write("{0}*({1}^{2})", coeffList[i], x, i);
                    }
                    if (!bFirst) {
                        Console.Write(" + ");
                    }
                }

                Console.Write(" { ");
                {
                    bool bFirst = true;
                    for (int i = 0; i < numerCoeffList.Count; ++i) {
                        if (numerCoeffList[i].AlmostZero()) {
                            continue;
                        }

                        if (bFirst) {
                            bFirst = false;
                        } else {
                            Console.Write(" + ");
                        }
                        Console.Write("{0}*({1}^{2})", numerCoeffList[i], x, i);
                    }
                }

                Console.Write(" } / { ");

                {
                    for (int i = 0; i < denomRootList.Count; ++i) {
                        if (denomRootList[i].AlmostZero()) {
                            Console.WriteLine(" {0} ", x);
                            continue;
                        }

                        Console.Write("({0}-({1}))", x, denomRootList[i]);
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
        Reduction(List<WWComplex> aNumerCoeffList, List<WWComplex> aDenomRootList) {
            var rv = new PolynomialAndRationalPolynomial();
            rv.coeffList = new List<WWComplex>();
            rv.numerCoeffList = new List<WWComplex>();
            for (int i = 0; i < aNumerCoeffList.Count; ++i) {
                rv.numerCoeffList.Add(new WWComplex(aNumerCoeffList[i]));
            }
            rv.denomRootList = new List<WWComplex>();
            for (int i = 0; i < aDenomRootList.Count; ++i) {
                rv.denomRootList.Add(new WWComplex(aDenomRootList[i]));
            }

            if (rv.numerCoeffList.Count - 1 < rv.denomRootList.Count) {
                // 既に分子の多項式の次数が分母の多項式の次数よりも低い。
                return rv;
            }

            // 分母の根のリスト⇒分母の多項式の係数のリスト
            var denomCoeffList = RootListToCoeffList(rv.denomRootList, new WWComplex(1, 0));

            while (denomCoeffList.Count <= rv.numerCoeffList.Count) {
                // denomの最も次数が高い項の係数がcdn、numerの最も次数が高い項の係数がcnnとすると
                // denomiCoeffListを-cnn/cdn * s^(numerの次数-denomの次数)倍してnumerCoeffListと足し合わせてnumerの次数を1下げる。
                // このときrv.coeffListにc == cnn/cdnを足す。
                var c = new WWComplex(rv.numerCoeffList[rv.numerCoeffList.Count - 1])
                            .Div(denomCoeffList[denomCoeffList.Count - 1]);
                rv.coeffList.Insert(0, c);
                var denomMulX = denomCoeffList;
                while (denomMulX.Count < rv.numerCoeffList.Count) {
                    denomMulX = MultiplyX(denomMulX);
                }
                denomMulX = MultiplyC(denomMulX, c.Mul(-1));

                // ここで引き算することで分子の多項式の次数が1減る。
                int expectedOrder = rv.numerCoeffList.Count - 2;
                rv.numerCoeffList = Add(denomMulX, rv.numerCoeffList);
                rv.numerCoeffList = TrimPolynomialOrder(rv.numerCoeffList, expectedOrder);

                // 引き算によって一挙に2以上次数が減った場合coeffListに0を足す。
                int actualOrder = rv.numerCoeffList.Count - 1;
                for (int i=0; i < expectedOrder - actualOrder; ++i) {
                    rv.coeffList.Insert(0, new WWComplex(0, 0));
                }
            }
            return rv;
        }

        /// <summary>
        /// p次オールポールの多項式(分子は多項式係数のリストで分母は根のリスト)を部分分数展開する。分子の多項式の次数はp次未満。
        ///
        ///             ... + nCoeffs[2]s^2 + nCoeffs[1]s + nCoeffs[0]
        /// F(s) = ----------------------------------------------------------
        ///          (s-dRoots[0])(s-dRoots[1])(s-dRoots[3])…(s-dRoots[p-1])
        ///          
        /// 戻り値は、多項式を部分分数分解した結果の、1次有理多項式のp個のリスト。
        /// 
        ///             c[0]            c[1]            c[2]             c[p-1]
        /// F(s) = ------------- + ------------- + ------------- + … + ---------------------
        ///         s-dRoots[0]     s-dRoots[1]     s-dRoots[2]         s-dRoots[p-1]
        /// </summary>
        public static List<FirstOrderRationalPolynomial> PartialFractionDecomposition(
                List<WWComplex> nCoeffs, List<WWComplex> dRoots) {
            var result = new List<FirstOrderRationalPolynomial>();

            if (dRoots.Count == 1 && nCoeffs.Count == 1) {
                result.Add(new FirstOrderRationalPolynomial(new WWComplex(0,0), nCoeffs[0], new WWComplex(1,0), WWComplex.Minus(dRoots[0])));
                return result;
            }

            if (dRoots.Count < 2) {
                throw new ArgumentException("dRoots");
            }
            if (dRoots.Count < nCoeffs.Count) {
                throw new ArgumentException("nCoeffs");
            }

            for (int k = 0; k < dRoots.Count; ++k) {
                // cn = ... + nCoeffs[2]s^2 + nCoeffs[1]s + nCoeffs[0]
                // 係数c[0]は、s==dRoots[0]としたときの、cn/(s-dRoots[1])(s-dRoots[2])(s-dRoots[3])…(s-dRoots[p-1])
                // 係数c[1]は、s==dRoots[1]としたときの、cn/(s-dRoots[0])(s-dRoots[2])(s-dRoots[3])…(s-dRoots[p-1])
                // 係数c[2]は、s==dRoots[2]としたときの、cn/(s-dRoots[0])(s-dRoots[1])(s-dRoots[3])…(s-dRoots[p-1])

                // 分子の値c。
                var c = new WWComplex(0, 0);
                var s = new WWComplex(1,0);
                for (int j = 0; j < nCoeffs.Count; ++j) {
                    c.Add(WWComplex.Mul(nCoeffs[j], s));
                    s.Mul(dRoots[k]);
                }

                for (int i = 0; i < dRoots.Count; ++i) {
                    if (i == k) {
                        continue;
                    }

                    c.Div(WWComplex.Sub(dRoots[k], dRoots[i]));
                }

                result.Add(new FirstOrderRationalPolynomial(new WWComplex(0,0), c, new WWComplex(1,0), WWComplex.Minus(dRoots[k])));
            }

            return result;
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
                var nPolynomialCoeffs = new List<WWComplex>();
                nPolynomialCoeffs.Add(new WWComplex(3, 0));
                nPolynomialCoeffs.Add(new WWComplex(1, 0));

                var dRoots = new List<WWComplex>();
                dRoots.Add(new WWComplex(-1, 0));
                dRoots.Add(new WWComplex(0, 0));
                dRoots.Add(new WWComplex(2, 0));

                var polynomialList = WWPolynomial.PartialFractionDecomposition(nPolynomialCoeffs, dRoots);

                for (int i = 0; i < polynomialList.Count; ++i) {
                    Console.WriteLine(polynomialList[i].ToString("s"));
                    if (i != polynomialList.Count - 1) {
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
                numerC.Add(WWComplex.Unity().Mul(-1)); // 定数項。
                numerC.Add(WWComplex.Unity());         // 1乗の項。

                var denomR = new List<WWComplex>();
                denomR.Add(WWComplex.Unity().Mul(-1));

                var r = Reduction(numerC, denomR);
                r.Print("x");
            }
        }
    }
}

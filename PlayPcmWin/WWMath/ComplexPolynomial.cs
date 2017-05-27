using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WWMath {
    public class ComplexPolynomial {
        /// <summary>
        /// coef[0]: constant, coef[1] 1st degree coef, ...
        /// </summary>
        private readonly WWComplex [] mCoeff;

        public ComplexPolynomial(WWComplex [] coeff) {
            mCoeff = new WWComplex[coeff.Length];
            Array.Copy(coeff, mCoeff, coeff.Length);
        }

        public WWComplex[] ToArray() {
            var r = new WWComplex[mCoeff.Length];
            Array.Copy(mCoeff, r, mCoeff.Length);
            return r;
        }

        public WWComplex[] Coeffs() {
            return mCoeff;
        }

        /// <summary>
        /// 2つの多項式の和を戻す。
        /// </summary>
        /// <param name="L">多項式の係数のリストl</param>
        /// <param name="R">多項式の係数のリストr</param>
        /// <returns>l+r</returns>
        public static ComplexPolynomial
        Add(ComplexPolynomial lhs, ComplexPolynomial rhs) {
            int order = lhs.Degree;
            if (order < rhs.Degree) {
                order = rhs.Degree;
            }

            var rv = new WWComplex[order+1];
            for (int i = 0; i <= order; ++i) {
                WWComplex ck = WWComplex.Zero();
                if (i <= lhs.Degree) {
                    ck = WWComplex.Add(ck, lhs.C(i));
                }
                if (i <= rhs.Degree) {
                    ck = WWComplex.Add(ck, rhs.C(i));
                }
                rv[i] = ck;
            }

            // 最高次の項が0のとき削除して次数を減らす。
            return new ComplexPolynomial(rv).ReduceDegree();
        }

        /// <summary>
        /// coefficient of nth degree element
        /// </summary>
        public WWComplex C(int nth) {
            return mCoeff[nth];
        }
        public int Degree {
            get {
                return mCoeff.Length - 1;
            }
        }

        /// <summary>
        /// 最大次数の係数を0でない状態にした配列を戻す。自分自身は変更しない。
        /// </summary>
        /// <returns>最大次数の係数が0ではない多項式の係数リスト。</returns>
        public ComplexPolynomial ReduceDegree() {
            // decide result degree
            int degree = 0;
            for (int i = mCoeff.Length - 1; 0 < i; --i) {
                if (!mCoeff[i].AlmostZero()) {
                    degree = i;
                    break;
                }
            }

            var r = new WWComplex[degree + 1];
            Array.Copy(mCoeff, r, degree + 1);
            return new ComplexPolynomial(r);
        }

        /// <summary>
        /// Calculate poly value at position z
        /// </summary>
        /// <param name="p">z position</param>
        /// <returns>y</returns>
        public WWComplex Evaluate(WWComplex z) {
            WWComplex y = WWComplex.Zero();
            WWComplex zPower = WWComplex.Unity();
            for (int i = 0; i < mCoeff.Length; ++i) {
                y = WWComplex.Add(y, WWComplex.Mul(mCoeff[i], zPower));
                zPower = WWComplex.Mul(zPower, z);
            }

            return y;
        }

        /// <summary>
        /// 多項式の係数のリスト(変数x)を受け取ってx倍して戻す。引数のcoeffListの内容は変更しない。
        /// </summary>
        public static ComplexPolynomial
        MultiplyX(ComplexPolynomial p) {
            var rv = new WWComplex [p.Degree+2];
            rv[0] = WWComplex.Zero();
            for (int i = 0; i <= p.Degree; ++i) {
                rv[i+1] = p.C(i);
            }
            return new ComplexPolynomial(rv);
        }

        /// <summary>
        /// 多項式の係数のリストを受け取り、全体をc倍して戻す。引数のcoeffListの内容は変更しない。
        /// </summary>
        public static ComplexPolynomial
        MultiplyC(ComplexPolynomial p, WWComplex c) {
            var rv = new WWComplex[p.Degree + 1];
            for (int i = 0; i <= p.Degree; ++i) {
                rv[i] = WWComplex.Mul(p.C(i), c);
            }
            return new ComplexPolynomial(rv);
        }

        /// <summary>
        /// 多項式の次数を強制的にn次にする。n+1次以上の項を消す
        /// </summary>
        /// <param name="p">多項式の係数のリスト。mCoeffs[0]:定数項、mCoeffs[1]:1次の項。</param>
        /// <param name="n">次数。0=定数のみ。1==定数と1次の項。</param>
        public static ComplexPolynomial
        TrimPolynomialOrder(ComplexPolynomial p, int n) {
            int newOrder = n;
            if (p.Degree < newOrder) {
                newOrder = p.Degree;
            }
            var rv = new WWComplex[newOrder + 1];
            for (int i = 0; i <= newOrder; ++i) {
                rv[i] = p.C(i);
            }

            //強制的に次数を減らした結果最高位の係数が0になったとき次数を減らす処理。
            return new ComplexPolynomial(rv).ReduceDegree();
        }

        public string ToString(string variableName) {
            var sb = new StringBuilder();
            bool bFirst = true;
            for (int i = mCoeff.Length - 1; 0 <= i; --i) {
                if (!C(i).AlmostZero()) {
                    if (!bFirst) {
                        sb.Append(" +");
                    } else {
                        bFirst = false;
                    }

                    if (i == 0) {
                        sb.AppendFormat(" {0}", C(i));
                    } else if (i == 1) {
                        sb.AppendFormat(" {0} * {1}", C(i), variableName);
                    } else {
                        sb.AppendFormat(" {0} * {1}^{2}", C(i), variableName, i);
                    }
                }
            }

            if (bFirst) {
                sb.Append("0");
            }

            return sb.ToString();
        }


        public override string ToString() {
            return ToString("z");
        }

    }
}

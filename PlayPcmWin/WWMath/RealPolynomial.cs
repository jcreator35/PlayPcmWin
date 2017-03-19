using System;
using System.Text;

namespace WWMath {
    /// <summary>
    /// 実係数多項式。係数は全て実数だが根の位置は複素数。変数は複素数でも良い。
    /// </summary>
    public class RealPolynomial {
        /// <summary>
        /// coef[0]: constant, coef[1] 1st degree coef, ...
        /// </summary>
        private readonly double [] mCoeff;

        public RealPolynomial(double [] coeff) {
            mCoeff = new double[coeff.Length];
            Array.Copy(coeff, mCoeff, coeff.Length);
        }

        /// <summary>
        /// coefficient of nth degree element. nth==0:定数項、nth==1:1次の項。
        /// </summary>
        public double C(int nth) {
            return mCoeff[nth];
        }

        public int Degree {
            get {
                return mCoeff.Length - 1;
            }
        }

        /// <summary>
        /// 最大次数の項が1になるように係数をスケールした多項式を戻す。
        /// 自分自身を変更しない。
        /// </summary>
        /// <returns>最大次数の項が1の多項式。</returns>
        public RealPolynomial Normalize() {
            var p = RemoveLeadingZeros();
            if (p.Degree == 0) {
                throw new ArithmeticException("polynomial degree is zero");
            }
            
            var c = new double[p.Degree + 1];
            c[p.Degree] = 1.0;
            for (int i=0; i < p.Degree; ++i) {
                c[i] = p.C(i) / p.C(p.Degree);
            }

            return new RealPolynomial(c);
        }

        private static bool AlmostZero(double v) {
            return Math.Abs(v) < double.Epsilon;
        }

        /// <summary>
        /// 多項式の係数をスケールする。
        /// </summary>
        public RealPolynomial Scale(double scale) {
            var c = new double[Degree + 1];
            for (int i=0; i < c.Length; ++i) {
                c[i] = C(i) * scale;
            }
            return new RealPolynomial(c);
        }

        /// <summary>
        /// 多項式の定数項にcを足した多項式を戻す。自分自身を変更しない。
        /// </summary>
        public RealPolynomial AddConstant(double c) {
            var newCoeffs = new double[Degree + 1];
            Array.Copy(mCoeff, newCoeffs, Degree + 1);
            newCoeffs[0] += c;
            return new RealPolynomial(newCoeffs);
        }

        /// <summary>
        /// 係数を右シフトする。自分自身は変更しない。
        /// count==1のとき1次の項→0次の項、2次の項→1次の項、…
        /// count==2のとき2次の項→0次の項、3次の項→1次の項、…
        /// </summary>
        /// <returns>係数が右シフトした多項式。</returns>
        public RealPolynomial RightShiftCoeffs(int count) {
            int degree = Degree - count;
            if (degree < 0) {
                degree = 0;
            }

            var c = new double[degree + 1];
            c[0] = 0;

            for (int i = 0; i <= Degree - count; ++i) {
                c[i] = C(i + count);
            }

            return new RealPolynomial(c);
        }

        /// <summary>
        /// 最大次数の係数を0でない状態にした配列を戻す。自分自身は変更しない。
        /// </summary>
        /// <returns>最大次数の係数が0ではない多項式の係数リスト。</returns>
        public RealPolynomial RemoveLeadingZeros() {
            // decide degree
            int order = 0;
            for (int i = mCoeff.Length - 1; 0 < i; --i) {
                if (!AlmostZero(mCoeff[i])) {
                    order = i;
                    break;
                }
            }

            var r = new double[order + 1];
            Array.Copy(mCoeff, r, order + 1);
            return new RealPolynomial(r);
        }

        /// <summary>
        /// calc derivative of all-real polynomial
        /// </summary>
        /// <returns>derivative</returns>
        public RealPolynomial Derivative() {
            if (mCoeff.Length == 1) {
                // 係数が定数項のみのとき
                var c = new double[1];
                c[0] = 0;
                return new RealPolynomial(c);
            }

            var deriv = new double[mCoeff.Length - 1];
            for (int i = 0; i < deriv.Length; ++i) {
                deriv[i] = (i + 1) * mCoeff[i + 1];
            }
            return new RealPolynomial(deriv);
        }

        /// <summary>
        /// Calculate all-real polynomial value at position p
        /// </summary>
        /// <param name="p">p position</param>
        /// <returns>y</returns>
        public double Evaluate(double x) {
            double y = 0;
            double xPower = 1;
            for (int i = 0; i < mCoeff.Length; ++i) {
                y += mCoeff[i] * xPower;
                xPower *= x;
            }

            return y;
        }

        /// <summary>
        /// この多項式が位置zで取る値を戻す。
        /// </summary>
        /// <param name="z">ガウス平面上の座標(p,y)</param>
        /// <returns>多項式が位置zで取る値。</returns>
        public WWComplex Evaluate(WWComplex z) {
            WWComplex w = WWComplex.Zero();
            WWComplex zPower = WWComplex.Unity();
            for (int i = 0; i < mCoeff.Length; ++i) {
                w = WWComplex.Add(w, zPower.Scale(mCoeff[i]));
                zPower = WWComplex.Mul(zPower, z);
            }

            return w;
        }
        
        public string ToString(string variableName) {
            var sb = new StringBuilder();
            bool bFirst = true;
            for (int i = mCoeff.Length - 1; 0 <= i; --i) {
                if (!AlmostZero(mCoeff[i])) {
                    if (!bFirst) {
                        sb.Append(" +");
                    } else {
                        bFirst = false;
                    }

                    if (i == 0) {
                        sb.AppendFormat(" {0}", mCoeff[i]);
                    } else if (i == 1) {
                        sb.AppendFormat(" {0} * {1}", mCoeff[i], variableName);
                    } else {
                        sb.AppendFormat(" {0} * {1}^{2}", mCoeff[i], variableName, i);
                    }
                }
            }

            if (bFirst) {
                sb.Append("0");
            }

            return sb.ToString();
        }

        public override string ToString() {
            return ToString("x");
        }
    }
}
